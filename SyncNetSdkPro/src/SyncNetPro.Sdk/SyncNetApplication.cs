using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using SyncNetPro.Sdk.Configuration;
using SyncNetPro.Sdk.Core;
using SyncNetPro.Sdk.Hosting;
using SyncNetPro.Sdk.Logging;
using SyncNetPro.Sdk.Nodes;
using SyncNetPro.Sdk.Remote;
using SyncNetPro.Sdk.Tracing;

namespace SyncNetPro.Sdk;

/// <summary>Titik masuk pembuatan host interface.</summary>
public static class SyncNetApplication
{
    /// <summary>
    /// Membuat host builder: konfigurasi berlapis (appsettings, env, argumen), berjalan sebagai
    /// systemd service di Linux dan Windows Service di Windows secara otomatis.
    /// </summary>
    public static HostApplicationBuilder CreateBuilder(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory,
        });
        builder.Services.AddSystemd();
        builder.Services.AddWindowsService();
        return builder;
    }
}

/// <summary>Registrasi SDK ke DI.</summary>
public static class SyncNetServiceCollectionExtensions
{
    /// <summary>
    /// Mendaftarkan interface <typeparamref name="THandler"/> beserta seluruh layanan SDK.
    /// Opsi dibaca dari bagian <c>SyncNet</c> lalu <paramref name="configure"/>.
    /// </summary>
    public static IHostApplicationBuilder AddSyncNetInterface<THandler>(this IHostApplicationBuilder builder, Action<SyncNetOptions>? configure = null)
        where THandler : SyncNetInterface
    {
        ArgumentNullException.ThrowIfNull(builder);
        IServiceCollection services = builder.Services;

        OptionsBuilder<SyncNetOptions> options = services.AddOptions<SyncNetOptions>()
            .Bind(builder.Configuration.GetSection(SyncNetOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(o => !string.IsNullOrWhiteSpace(o.Core.Host), "SyncNet:Core:Host wajib diisi.")
            .Validate(o => o.NodeSource != NodeSourceKind.Json || o.Nodes.All(n => !string.IsNullOrWhiteSpace(n.Name)), "Setiap SyncNet:Nodes wajib memiliki Name.")
            .ValidateOnStart();
        if (configure is not null) options.Configure(configure);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton(sp => LegacyCoreConfiguration.Load(sp.GetRequiredService<IOptions<SyncNetOptions>>().Value));
        services.TryAddSingleton(sp =>
        {
            string connectionString = sp.GetRequiredService<CoreEnvironment>().ConnectionString
                ?? throw new InvalidOperationException("Connection string database tidak tersedia.");
            return NpgsqlDataSource.Create(connectionString);
        });

        services.TryAddSingleton<INodeConfigurationSource>(sp =>
            sp.GetRequiredService<IOptions<SyncNetOptions>>().Value.NodeSource == NodeSourceKind.Json
                ? ActivatorUtilities.CreateInstance<JsonNodeConfigurationSource>(sp)
                : ActivatorUtilities.CreateInstance<PostgresNodeConfigurationSource>(sp));
        services.TryAddSingleton<INodeStatusReporter>(sp =>
        {
            SyncNetOptions o = sp.GetRequiredService<IOptions<SyncNetOptions>>().Value;
            return o.NodeSource == NodeSourceKind.Database && o.Database.ReportStatus && sp.GetRequiredService<CoreEnvironment>().ConnectionString is not null
                ? ActivatorUtilities.CreateInstance<PostgresNodeStatusReporter>(sp)
                : ActivatorUtilities.CreateInstance<LoggingNodeStatusReporter>(sp);
        });
        services.TryAddSingleton<INodeRegistry, NodeRegistry>();
        services.TryAddSingleton<ICoreCorrelationKeyProvider, CoreSwitchKeyProvider>();

        services.TryAddSingleton<TraceWriter>();
        services.TryAddSingleton<ITraceWriter>(sp => sp.GetRequiredService<TraceWriter>());
        services.TryAddSingleton<TraceSinkFactory>();
        services.TryAddSingleton<StartupSignal>();

        services.TryAddSingleton<THandler>();
        services.TryAddSingleton<SyncNetInterface>(sp => sp.GetRequiredService<THandler>());
        services.TryAddSingleton<SyncNetServices>();
        services.TryAddSingleton<CoreChannelManager>();
        services.TryAddSingleton<RemoteConnectionManager>();
        services.TryAddSingleton<IRemoteRegistry>(sp => sp.GetRequiredService<RemoteConnectionManager>());
        services.TryAddSingleton<ICoreClient>(sp => sp.GetRequiredService<CoreChannelManager>());
        services.TryAddSingleton<SyncNetRuntime>();
        services.TryAddSingleton<ISyncNetRuntime>(sp => sp.GetRequiredService<SyncNetRuntime>());

        services.AddHostedService(sp => sp.GetRequiredService<SyncNetRuntime>());
        services.AddHostedService<TraceDispatcher>();

        services.AddSingleton<ILoggerProvider>(sp =>
        {
            SyncNetOptions o = sp.GetRequiredService<IOptions<SyncNetOptions>>().Value;
            if (!o.Logging.Enabled) return Microsoft.Extensions.Logging.Abstractions.NullLoggerProvider.Instance;
            CoreEnvironment environment = sp.GetRequiredService<CoreEnvironment>();
            TraceWriter trace = sp.GetRequiredService<TraceWriter>();
            return new SyncNetFileLoggerProvider(environment.LogDirectory, o.AppName, o.Logging.LegacyWindowsFileNames,
                o.Logging.ForwardToTrace, () => trace, sp.GetRequiredService<TimeProvider>());
        });

        services.AddHealthChecks().AddCheck<CoreChannelHealthCheck>("syncnet-core");
        return builder;
    }
}
