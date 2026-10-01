using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SyncNetPro.Sdk.Commands;
using SyncNetPro.Sdk.Core;
using SyncNetPro.Sdk.Nodes;
using SyncNetPro.Sdk.Remote;

namespace SyncNetPro.Sdk.Hosting;

/// <summary>
/// Orkestrasi siklus hidup interface (pengganti <c>AppProcessor.AppStart/AppStop</c>):
/// muat konfigurasi → status UP → kanal Core → koneksi eksternal → command port → <see cref="SyncNetInterface.OnStartedAsync"/>.
/// Berhenti dengan urutan terbalik: koneksi eksternal ditutup lebih dulu (request HTTP yang sedang berjalan
/// diselesaikan Kestrel), lalu kanal Core setelah request yang sedang diproses selesai.
/// </summary>
public sealed class SyncNetRuntime : IHostedService, ISyncNetRuntime, IAsyncDisposable
{
    private readonly IOptions<SyncNetOptions> _options;
    private readonly SyncNetInterface _handler;
    private readonly INodeRegistry _registry;
    private readonly INodeStatusReporter _status;
    private readonly CoreChannelManager _channels;
    private readonly RemoteConnectionManager _remote;
    private readonly StartupSignal _startup;
    private readonly ILogger<SyncNetRuntime> _logger;
    private readonly CommandServer _commands;
    private readonly ISyncNetModule[] _modules;

    /// <summary>Membuat runtime.</summary>
    public SyncNetRuntime(
        IOptions<SyncNetOptions> options,
        SyncNetInterface handler,
        INodeRegistry registry,
        INodeStatusReporter status,
        CoreChannelManager channels,
        RemoteConnectionManager remote,
        StartupSignal startup,
        SyncNetServices services,
        ILoggerFactory loggerFactory,
        IEnumerable<ISyncNetModule>? modules = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
        _handler = handler;
        _registry = registry;
        _status = status;
        _channels = channels;
        _remote = remote;
        _startup = startup;
        _logger = loggerFactory.CreateLogger<SyncNetRuntime>();
        _modules = [.. modules ?? []];
        Version = options.Value.Version ?? DefaultVersion();
        _commands = new CommandServer(options, Version, handler, registry, services, loggerFactory, ReloadAsync);
    }

    /// <inheritdoc />
    public string AppName => _options.Value.AppName;

    /// <inheritdoc />
    public string Version { get; }

    /// <summary>Alamat command port yang sedang didengarkan.</summary>
    public System.Net.IPEndPoint? CommandEndPoint => _commands.LocalEndPoint;

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        NodeConfiguration configuration;
        try
        {
            configuration = await _registry.ReloadAsync(cancellationToken).ConfigureAwait(false);
            _startup.SetConfigurationLoaded();
        }
        catch (Exception ex)
        {
            _startup.SetFailed(ex);
            _logger.LogCritical(ex, "{AppName} gagal memuat konfigurasi", AppName);
            throw;
        }

        foreach (ISyncNetModule module in _modules) await module.StartAsync(cancellationToken).ConfigureAwait(false);

        await _status.ReportApplicationAsync(AppName, true, cancellationToken).ConfigureAwait(false);
        await _channels.ReconcileAsync(configuration, cancellationToken).ConfigureAwait(false);
        await _remote.ReconcileAsync(configuration, cancellationToken).ConfigureAwait(false);

        if (_options.Value.Command.Enabled)
        {
            int? port = _options.Value.Command.Port ?? configuration.CommandPort;
            if (port is null) _logger.LogWarning("Command port tidak dikonfigurasi (sw_app.command_port / SyncNet:Command:Port)");
            else await _commands.StartAsync(port.Value, cancellationToken).ConfigureAwait(false);
        }

        await _handler.OnStartedAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("{AppName} {Version} started", AppName, Version);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("{AppName} stop", AppName);
        try
        {
            await _handler.OnStoppingAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnStoppingAsync gagal");
        }

        await _commands.StopAsync().ConfigureAwait(false);
        await _status.ReportApplicationAsync(AppName, false, CancellationToken.None).ConfigureAwait(false);
        await _remote.StopAsync().ConfigureAwait(false);
        await _channels.StopAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);

        foreach (ISyncNetModule module in _modules)
        {
            try
            {
                await module.StopAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Modul {Module} gagal berhenti", module.GetType().Name);
            }
        }
    }

    /// <inheritdoc />
    public async Task<NodeConfiguration> ReloadAsync(CancellationToken cancellationToken = default)
    {
        NodeConfiguration configuration = await _registry.ReloadAsync(cancellationToken).ConfigureAwait(false);
        await _remote.ReconcileAsync(configuration, cancellationToken).ConfigureAwait(false);
        await _channels.ReconcileAsync(configuration, cancellationToken).ConfigureAwait(false);
        foreach (ISyncNetModule module in _modules) await module.ReloadAsync(cancellationToken).ConfigureAwait(false);
        await _handler.OnConfigurationReloadedAsync(configuration, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("{AppName} resync selesai", AppName);
        return configuration;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _commands.DisposeAsync();

    private static string DefaultVersion()
    {
        string? version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrEmpty(version)) return "1.0.0";
        int plus = version.IndexOf('+', StringComparison.Ordinal);
        return plus > 0 ? version[..plus] : version;
    }
}
