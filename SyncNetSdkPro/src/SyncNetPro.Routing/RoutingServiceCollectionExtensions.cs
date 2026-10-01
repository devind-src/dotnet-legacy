using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using SyncNetPro.Sdk.Configuration;
using SyncNetPro.Sdk.Hosting;

namespace SyncNetPro.Routing;

/// <summary>Opsi modul routing (<c>SyncNet:Routing</c>).</summary>
public sealed class RoutingOptions
{
    /// <summary>Nama section.</summary>
    public const string SectionName = "SyncNet:Routing";

    /// <summary>Connection string database Core; kosong = sama dengan konfigurasi node (config Core / <c>SyncNet:Database</c>).</summary>
    public string? ConnectionString { get; set; }

    /// <summary>Interval flush volume Volume &amp; Tiering (default 5 detik, sama dengan SDK lama).</summary>
    public TimeSpan VolumeFlushInterval { get; set; } = TimeSpan.FromSeconds(5);
}

/// <summary>Memuat data routing &amp; fee saat start/RESYNC dan menulis volume tersisa saat berhenti.</summary>
internal sealed class RoutingModule(ProductFeeCalculator fees, PriceBook prices, RoutingResolver resolver, ILogger<RoutingModule> logger) : ISyncNetModule
{
    public Task StartAsync(CancellationToken cancellationToken) => LoadAsync(cancellationToken);

    public Task ReloadAsync(CancellationToken cancellationToken) => LoadAsync(cancellationToken);

    // Dipanggil setelah kanal ditutup, sehingga volume transaksi terakhir ikut tersimpan
    // (ApiChannel lama melakukan flush sebelum kanal berhenti).
    public Task StopAsync(CancellationToken cancellationToken) => resolver.FlushVolumeAsync(cancellationToken);

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        await fees.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await prices.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await resolver.InitializeAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Data routing & fee dimuat");
    }
}

/// <summary>Registrasi modul routing.</summary>
public static class RoutingServiceCollectionExtensions
{
    /// <summary>
    /// Mendaftarkan <see cref="RoutingResolver"/>, <see cref="ProductFeeCalculator"/>, <see cref="MarginCalculator"/>,
    /// <see cref="PriceBook"/>, dan komponennya dengan store PostgreSQL (database Core). Data dimuat otomatis saat start dan RESYNC.
    /// </summary>
    public static IHostApplicationBuilder AddSyncNetRouting(this IHostApplicationBuilder builder, Action<RoutingOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddSyncNetRouting(configure);
        return builder;
    }

    /// <inheritdoc cref="AddSyncNetRouting(IHostApplicationBuilder, Action{RoutingOptions}?)"/>
    public static IServiceCollection AddSyncNetRouting(this IServiceCollection services, Action<RoutingOptions>? configure = null)
    {
        OptionsBuilder<RoutingOptions> options = services.AddOptions<RoutingOptions>().BindConfiguration(RoutingOptions.SectionName);
        if (configure is not null) options.Configure(configure);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton(sp =>
        {
            string connectionString = sp.GetRequiredService<IOptions<RoutingOptions>>().Value.ConnectionString
                ?? sp.GetRequiredService<CoreEnvironment>().ConnectionString
                ?? throw new InvalidOperationException("Connection string database Core untuk routing tidak ditemukan (SyncNet:Routing:ConnectionString atau config Core).");
            return new RoutingDataSource(NpgsqlDataSource.Create(connectionString));
        });
        services.TryAddSingleton(sp => new PostgresRoutingStore(sp.GetRequiredService<RoutingDataSource>().DataSource,
            sp.GetRequiredService<TimeProvider>(), sp.GetRequiredService<ILogger<PostgresRoutingStore>>()));
        services.TryAddSingleton<ISupplierHealthStore>(sp => sp.GetRequiredService<PostgresRoutingStore>());
        services.TryAddSingleton<IRoutingScheduleStore>(sp => sp.GetRequiredService<PostgresRoutingStore>());
        services.TryAddSingleton<ICommitmentStore>(sp => sp.GetRequiredService<PostgresRoutingStore>());
        services.TryAddSingleton<IRoutingCycleStore>(sp => sp.GetRequiredService<PostgresRoutingStore>());
        services.TryAddSingleton<IRoutingDataStore>(sp => sp.GetRequiredService<PostgresRoutingStore>());

        services.TryAddSingleton<SupplierHealthTracker>();
        services.TryAddSingleton<RoutingSchedule>();
        services.TryAddSingleton(sp => new CommitmentTracker(sp.GetRequiredService<ICommitmentStore>(), sp.GetRequiredService<TimeProvider>(),
            sp.GetRequiredService<IOptions<RoutingOptions>>().Value.VolumeFlushInterval, sp.GetRequiredService<ILogger<CommitmentTracker>>()));
        services.TryAddSingleton<PriceBook>();
        services.TryAddSingleton<MarginCalculator>();
        services.TryAddSingleton<ProductFeeCalculator>();
        services.TryAddSingleton<StaticRouting>();
        services.TryAddSingleton(sp => new MarginRouting(sp.GetRequiredService<IRoutingDataStore>(), sp.GetRequiredService<PriceBook>(),
            sp.GetRequiredService<SupplierHealthTracker>(), sp.GetRequiredService<RoutingSchedule>(), sp.GetRequiredService<CommitmentTracker>()));
        services.TryAddSingleton(sp => new ProductRouting(sp.GetRequiredService<IRoutingDataStore>(), sp.GetRequiredService<SupplierHealthTracker>(),
            sp.GetRequiredService<RoutingSchedule>(), sp.GetRequiredService<CommitmentTracker>()));
        services.TryAddSingleton(sp => new RoutingResolver(sp.GetRequiredService<StaticRouting>(), sp.GetRequiredService<MarginRouting>(),
            sp.GetRequiredService<ProductRouting>(), sp.GetRequiredService<SupplierHealthTracker>(), sp.GetRequiredService<RoutingSchedule>(),
            sp.GetRequiredService<IRoutingCycleStore>(), sp.GetRequiredService<ILogger<RoutingResolver>>(), sp.GetRequiredService<CommitmentTracker>()));
        services.AddSingleton<ISyncNetModule, RoutingModule>();
        return services;
    }

    /// <summary>Pembungkus agar data source milik modul routing di-dispose bersama container.</summary>
    internal sealed class RoutingDataSource(NpgsqlDataSource dataSource) : IAsyncDisposable
    {
        public NpgsqlDataSource DataSource { get; } = dataSource;

        public ValueTask DisposeAsync() => DataSource.DisposeAsync();
    }
}
