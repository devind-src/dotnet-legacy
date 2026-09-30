using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SyncNetPro.Sdk.Core;
using SyncNetPro.Sdk.Hosting;
using SyncNetPro.Sdk.Nodes;
using SyncNetPro.Sdk.Tracing;

namespace SyncNetPro.Sdk.Testing;

/// <summary>
/// Menjalankan interface nyata (host SDK lengkap) yang otomatis tersambung ke <see cref="SimCore"/> —
/// untuk unit/integration test interface tanpa Core dan database.
/// </summary>
/// <example>
/// <code>
/// await using var core = await SimCore.StartAsync(options);
/// await using var app = await SimInterfaceHost.StartAsync&lt;MyInterface&gt;(core);
/// SimResult result = await core.SendAsync("BILLER", request);
/// </code>
/// </example>
public sealed class SimInterfaceHost<THandler> : IAsyncDisposable
    where THandler : SyncNetInterface
{
    private readonly IHost _host;
    private readonly string _home;

    internal SimInterfaceHost(IHost host, string home)
    {
        _host = host;
        _home = home;
    }

    /// <summary>Service provider interface.</summary>
    public IServiceProvider Services => _host.Services;

    /// <summary>Instance handler.</summary>
    public THandler Handler => Services.GetRequiredService<THandler>();

    /// <summary>Port command interface.</summary>
    public int CommandPort => Services.GetRequiredService<SyncNetRuntime>().CommandEndPoint?.Port ?? 0;

    /// <summary>Folder home sementara (log &amp; trace fallback).</summary>
    public string Home => _home;

    /// <summary>Menghentikan interface.</summary>
    public Task StopAsync() => _host.StopAsync();

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            await _host.StopAsync().ConfigureAwait(false);
        }
        finally
        {
            _host.Dispose();
            try
            {
                Directory.Delete(_home, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}

/// <summary>Pembuat <see cref="SimInterfaceHost{THandler}"/>.</summary>
public static class SimInterfaceHost
{
    /// <summary>
    /// Menjalankan interface <typeparamref name="THandler"/> dalam mode JSON dengan node dan port dari SimCore,
    /// trace ke Log Services tiruan (bila aktif), command port acak, dan <see cref="SimCoreOptions.Interface"/> diisi otomatis.
    /// </summary>
    /// <param name="core">SimCore.</param>
    /// <param name="connections">Koneksi eksternal (mis. ke <see cref="RemoteStub"/>).</param>
    /// <param name="configure">Penyesuaian opsi.</param>
    /// <param name="services">Registrasi layanan tambahan (sebelum SDK).</param>
    /// <param name="waitForConnection">Tunggu semua kanal Core terkoneksi.</param>
    public static async Task<SimInterfaceHost<THandler>> StartAsync<THandler>(
        SimCore core,
        IEnumerable<RemoteConnectionInfo>? connections = null,
        Action<SyncNetOptions>? configure = null,
        Action<IServiceCollection>? services = null,
        bool waitForConnection = true)
        where THandler : SyncNetInterface
    {
        ArgumentNullException.ThrowIfNull(core);
        string home = Path.Combine(Path.GetTempPath(), "simcore-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(home);

        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        services?.Invoke(builder.Services);
        builder.AddSyncNetInterface<THandler>(o =>
        {
            o.AppName = core.Options.AppName;
            o.Home = home;
            o.NodeSource = NodeSourceKind.Json;
            o.Core.Host = core.Options.BindAddress;
            o.Core.ReconnectDelay = TimeSpan.FromMilliseconds(200);
            o.Command.BindAddress = "127.0.0.1";
            o.Command.Port = 0;
            o.Remote.AutoSignOnDelay = TimeSpan.FromMilliseconds(100);
            o.Nodes = [.. core.Nodes.Select(n => new NodeInfo
            {
                Name = n.Name,
                Category = n.Options.Category,
                PortIn = n.SourcePort,
                PortOut = n.SinkPort,
                RequestTimeoutSeconds = n.Options.RequestTimeoutSeconds,
                AdviceTimeoutSeconds = n.Options.AdviceTimeoutSeconds,
            })];
            o.Connections = [.. connections ?? []];
            if (core.LogServicesPort is int logPort)
            {
                o.Trace.LogServicesHost = core.Options.BindAddress;
                o.Trace.LogServicesPort = logPort;
            }

            configure?.Invoke(o);
        });

        IHost host = builder.Build();
        await host.StartAsync().ConfigureAwait(false);
        var app = new SimInterfaceHost<THandler>(host, home);
        core.Options.Interface = new SimCommandTarget { Host = "127.0.0.1", Port = app.CommandPort };

        if (waitForConnection)
        {
            // Tunggu dari kedua sisi: SimCore sudah menerima koneksi dan interface sudah menandai kanalnya terkoneksi.
            var client = app.Services.GetRequiredService<ICoreClient>();
            foreach (SimNode node in core.Nodes)
            {
                await core.WaitForConnectionAsync(node.Name, TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                DateTime deadline = DateTime.UtcNow.AddSeconds(10);
                while ((node.Sink is not null && !client.IsConnected(node.Name, CoreChannelDirection.Outbound))
                    || (node.Source is not null && !client.IsConnected(node.Name, CoreChannelDirection.Inbound)))
                {
                    if (DateTime.UtcNow > deadline) throw new TimeoutException($"Interface belum menandai kanal {node.Name} terkoneksi.");
                    await Task.Delay(10).ConfigureAwait(false);
                }
            }

            // Trace sebelum Log Services terkoneksi masuk ke file fallback; tunggu agar test dapat memeriksa core.Traces.
            if (core.LogServicesPort is not null)
            {
                var traces = app.Services.GetRequiredService<TraceDispatcher>();
                DateTime deadline = DateTime.UtcNow.AddSeconds(10);
                while (!traces.IsPrimaryReady)
                {
                    if (DateTime.UtcNow > deadline) throw new TimeoutException("Interface belum terkoneksi ke Log Services SimCore.");
                    await Task.Delay(10).ConfigureAwait(false);
                }
            }
        }

        return app;
    }
}
