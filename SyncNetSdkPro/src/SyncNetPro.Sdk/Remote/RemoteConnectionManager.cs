using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SyncNetPro.Sdk.Logging;
using SyncNetPro.Sdk.Nodes;
using SyncNetPro.Sdk.Transport;

namespace SyncNetPro.Sdk.Remote;

/// <summary>
/// Mengelola koneksi ke sistem eksternal (pengganti <c>NodeRemotes/NodeRemote</c>): membuka koneksi per
/// <c>sw_connections</c>, korelasi balasan, dispatch pesan ke handler, auto sign-on, timer echo/key exchange,
/// dan pelaporan status.
/// </summary>
public sealed class RemoteConnectionManager : IRemoteRegistry, IAsyncDisposable, IRemoteConnectionEvents, IHttpRequestDispatcher
{
    private readonly SyncNetOptions _options;
    private readonly SyncNetInterface _handler;
    private readonly INodeStatusReporter _status;
    private readonly SyncNetServices _services;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly TimeProvider _time;
    private readonly SemaphoreSlim _reconcileLock = new(1, 1);
    private readonly ConcurrentDictionary<string, RemoteConnectionBase> _connections = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _limits = new(StringComparer.Ordinal);
    private readonly CancellationTokenSource _stopping = new();
    private CancellationTokenSource? _timers;
    private List<Task> _timerTasks = [];
    private IReadOnlyList<NodeInfo> _nodes = [];

    /// <summary>Membuat manager.</summary>
    public RemoteConnectionManager(IOptions<SyncNetOptions> options, SyncNetInterface handler, INodeStatusReporter status,
        SyncNetServices services, ILoggerFactory loggerFactory, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _handler = handler;
        _status = status;
        _services = services;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<RemoteConnectionManager>();
        _time = time;
    }

    /// <summary>Koneksi aktif.</summary>
    public IReadOnlyList<IRemoteConnection> Connections => [.. _connections.Values];

    /// <inheritdoc />
    public IRemoteNode GetNode(string nodeName) =>
        new RemoteNode(nodeName, [.. _connections.Values.Where(c => c.Node.Name == nodeName).OrderBy(c => c.Info.Name, StringComparer.Ordinal)]);

    /// <summary>Menyesuaikan koneksi dengan konfigurasi (buka baru, tutup yang dihapus, buka ulang yang berubah).</summary>
    public async Task ReconcileAsync(NodeConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        await _reconcileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var nodes = configuration.Nodes.ToDictionary(n => n.Name, StringComparer.Ordinal);
            var wanted = configuration.Connections
                .Where(c => nodes.ContainsKey(c.NodeName) && c.Protocol is not ConnectionProtocol.MessageQueue and not ConnectionProtocol.Custom)
                .ToDictionary(c => c.Name, StringComparer.Ordinal);

            foreach (RemoteConnectionInfo skipped in configuration.Connections.Where(c => c.Protocol is ConnectionProtocol.MessageQueue or ConnectionProtocol.Custom))
            {
                _logger.LogWarning("Koneksi {Connection}: protokol {Protocol} belum didukung", skipped.Name, skipped.Protocol);
            }

            var closedNodes = new HashSet<string>(StringComparer.Ordinal);
            foreach ((string name, RemoteConnectionBase connection) in _connections.ToArray())
            {
                if (wanted.TryGetValue(name, out RemoteConnectionInfo? info) && info == connection.Info)
                {
                    connection.Node = nodes[info.NodeName];
                    continue;
                }

                _connections.TryRemove(name, out _);
                closedNodes.Add(connection.Node.Name);
                await CloseAsync(connection).ConfigureAwait(false);
            }

            var openedNodes = new HashSet<string>(StringComparer.Ordinal);
            foreach (RemoteConnectionInfo info in wanted.Values.Where(c => !_connections.ContainsKey(c.Name)))
            {
                NodeInfo node = nodes[info.NodeName];
                try
                {
                    RemoteConnectionBase connection = Create(info, node);
                    _connections[info.Name] = connection;
                    await connection.StartAsync(_stopping.Token).ConfigureAwait(false);
                    openedNodes.Add(node.Name);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _connections.TryRemove(info.Name, out _);
                    _logger.LogError(ex, "Koneksi {Connection} ({Node}) gagal dibuka", info.Name, info.NodeName);
                }
            }

            foreach (string node in openedNodes) await _status.ReportRemoteNodeAsync(node, true, cancellationToken).ConfigureAwait(false);
            foreach (string node in closedNodes.Where(n => !_connections.Values.Any(c => c.Node.Name == n)))
            {
                await _status.ReportRemoteNodeAsync(node, false, cancellationToken).ConfigureAwait(false);
            }

            _nodes = configuration.Nodes;
            await RestartTimersAsync().ConfigureAwait(false);
        }
        finally
        {
            _reconcileLock.Release();
        }
    }

    /// <summary>Menutup semua koneksi dan timer.</summary>
    public async Task StopAsync()
    {
        await StopTimersAsync().ConfigureAwait(false);
        await _stopping.CancelAsync().ConfigureAwait(false);

        var nodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (RemoteConnectionBase connection in _connections.Values)
        {
            nodes.Add(connection.Node.Name);
            await CloseAsync(connection).ConfigureAwait(false);
        }

        _connections.Clear();
        foreach (string node in nodes) await _status.ReportRemoteNodeAsync(node, false).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_stopping.IsCancellationRequested) await StopAsync().ConfigureAwait(false);
        _stopping.Dispose();
        _reconcileLock.Dispose();
        foreach (SemaphoreSlim limit in _limits.Values) limit.Dispose();
    }

    private RemoteConnectionBase Create(RemoteConnectionInfo info, NodeInfo node)
    {
        ILogger logger = _loggerFactory.CreateLogger($"SyncNetPro.Sdk.Remote.{info.Name}");
        if (info.Protocol == ConnectionProtocol.WebService)
        {
            return info.Role == ConnectionRole.Server
                ? new HttpServerConnection(info, node, this, logger)
                : new HttpClientConnection(info, node, _options.Remote.AllowUntrustedCertificates, _options.Remote.ConnectTimeout, logger);
        }

        ITcpFrameCodec codec = _handler.CreateTcpCodec(info) ?? RemoteCodecs.ForConnection(info);
        if (info.Role == ConnectionRole.Server) return new TcpServerConnection(info, node, codec, this, logger, _time);
        return info.AlwaysConnected
            ? new PersistentTcpClientConnection(info, node, codec, this, _options.Remote.ConnectTimeout, logger, _time)
            : new NonPersistentTcpClientConnection(info, node, codec, this, _options.Remote.ConnectTimeout, logger, _time);
    }

    private async Task CloseAsync(RemoteConnectionBase connection)
    {
        _logger.LogInformation("Menutup koneksi {Connection} ({Node})", connection.Info.Name, connection.Node.Name);
        try
        {
            await connection.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Gagal menutup koneksi {Connection}: {Error}", connection.Info.Name, ex.Message);
        }

        if (connection.ReportsSocketStatus) await _status.ReportConnectionAsync(connection.Info.Name, false).ConfigureAwait(false);
    }

    // ---------------- Event koneksi ----------------

    Task IRemoteConnectionEvents.OnConnectedAsync(RemoteConnectionBase connection, EndPoint? remote)
    {
        NodeInfo node = connection.Node;
        _logger.LogInformation("Interchange {Node} ({Connection}) connected at address {Remote}", node.Name, connection.Info.Name, remote);
        _ = RunHandlerAsync(node, async (logger, ct) =>
        {
            if (connection.ReportsSocketStatus) await _status.ReportConnectionAsync(connection.Info.Name, true, ct).ConfigureAwait(false);
            var context = new RemoteConnectionContext(node, connection, remote, _services, logger);
            await _handler.OnRemoteConnectedAsync(context, ct).ConfigureAwait(false);

            if (node.AutoSignOn)
            {
                await Task.Delay(_options.Remote.AutoSignOnDelay, _time, ct).ConfigureAwait(false);
                await _handler.OnAutoSignOnAsync(context, ct).ConfigureAwait(false);
            }
        }, "OnRemoteConnectedAsync/OnAutoSignOnAsync", limited: false);
        return Task.CompletedTask;
    }

    Task IRemoteConnectionEvents.OnDisconnectedAsync(RemoteConnectionBase connection, EndPoint? remote)
    {
        NodeInfo node = connection.Node;
        _logger.LogInformation("Interchange {Node} ({Connection}) disconnected at address {Remote}", node.Name, connection.Info.Name, remote);
        _ = RunHandlerAsync(node, async (logger, ct) =>
        {
            if (connection.ReportsSocketStatus) await _status.ReportConnectionAsync(connection.Info.Name, false, CancellationToken.None).ConfigureAwait(false);
            await _handler.OnRemoteDisconnectedAsync(new RemoteConnectionContext(node, connection, remote, _services, logger), ct).ConfigureAwait(false);
        }, "OnRemoteDisconnectedAsync", limited: false);
        return Task.CompletedTask;
    }

    async Task IRemoteConnectionEvents.OnTcpFrameAsync(RemoteConnectionBase connection, byte[] payload, EndPoint? remote,
        Func<ReadOnlyMemory<byte>, CancellationToken, Task> reply)
    {
        NodeInfo node = connection.Node;
        SemaphoreSlim limit = _limits.GetOrAdd(node.Name, _ => new SemaphoreSlim(_options.MaxConcurrentRequestsPerNode));
        await limit.WaitAsync(_stopping.Token).ConfigureAwait(false);
        _ = RunHandlerAsync(node, (logger, ct) =>
                _handler.OnRemoteMessageAsync(new RemoteMessageContext(node, connection, payload, remote, reply, _services, logger), ct),
            "OnRemoteMessageAsync", limited: true);
    }

    string? IRemoteConnectionEvents.GetCorrelationKey(RemoteConnectionInfo connection, byte[] payload)
    {
        try
        {
            return _handler.GetRemoteCorrelationKey(connection, payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("GetRemoteCorrelationKey gagal untuk {Connection}: {Error}", connection.Name, ex.Message);
            return null;
        }
    }

    async Task<HttpReply> IHttpRequestDispatcher.DispatchAsync(HttpServerConnection connection, HttpContext http, CancellationToken cancellationToken)
    {
        NodeInfo node = connection.Node;
        string body;
        using (var reader = new StreamReader(http.Request.Body, Encoding.UTF8))
        {
            body = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        }

        var headers = http.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        ILogger logger = _loggerFactory.CreateLogger(_handler.GetType());
        using IDisposable? scope = logger.BeginNodeScope(node.Name);
        var context = new HttpRequestContext(node, connection, http.Request.Method, http.Request.Path.Value ?? "/", http.Request.QueryString.Value ?? string.Empty,
            headers, body, http.Request.ContentType, http.Connection.RemoteIpAddress?.ToString() ?? string.Empty, _services, logger);

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stopping.Token);
        return await _handler.OnHttpRequestAsync(context, linked.Token).ConfigureAwait(false);
    }

    private Task RunHandlerAsync(NodeInfo node, Func<ILogger, CancellationToken, Task> action, string callback, bool limited) =>
        Task.Run(async () =>
        {
            ILogger logger = _loggerFactory.CreateLogger(_handler.GetType());
            using IDisposable? scope = logger.BeginNodeScope(node.Name);
            try
            {
                await action(logger, _stopping.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_stopping.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{Callback} gagal untuk node {Node}", callback, node.Name);
            }
            finally
            {
                if (limited && _limits.TryGetValue(node.Name, out SemaphoreSlim? limit)) limit.Release();
            }
        });

    // ---------------- Timer ----------------

    private async Task RestartTimersAsync()
    {
        await StopTimersAsync().ConfigureAwait(false);
        _timers = CancellationTokenSource.CreateLinkedTokenSource(_stopping.Token);
        CancellationToken ct = _timers.Token;
        var tasks = new List<Task> { EveryAsync(_options.Remote.StatusInterval, ReportStatusAsync, ct) };

        foreach (NodeInfo node in _nodes.Where(n => _connections.Values.Any(c => c.Node.Name == n.Name)))
        {
            if (node.EchoTimerMinutes > 0)
            {
                tasks.Add(EveryAsync(TimeSpan.FromMinutes(node.EchoTimerMinutes),
                    token => RunHandlerAsync(node, (logger, t) => _handler.OnEchoTimerAsync(new NodeContext(node, _services, logger), t), "OnEchoTimerAsync", false), ct));
            }

            if (node.KeyChangeTimerMinutes > 0)
            {
                tasks.Add(EveryAsync(TimeSpan.FromMinutes(node.KeyChangeTimerMinutes),
                    token => RunHandlerAsync(node, (logger, t) => _handler.OnKeyExchangeTimerAsync(new NodeContext(node, _services, logger), t), "OnKeyExchangeTimerAsync", false), ct));
            }
        }

        _timerTasks = tasks;
    }

    private async Task StopTimersAsync()
    {
        if (_timers is null) return;
        await _timers.CancelAsync().ConfigureAwait(false);
        try
        {
            await Task.WhenAll(_timerTasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }

        _timers.Dispose();
        _timers = null;
        _timerTasks = [];
    }

    private async Task EveryAsync(TimeSpan period, Func<CancellationToken, Task> action, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(period, _time);
        try
        {
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                try
                {
                    await action(ct).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Timer gagal");
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <summary>Status koneksi tiap interval (setara <c>TimerUpdateNode</c> lama); klien non-persistent dilewati.</summary>
    private async Task ReportStatusAsync(CancellationToken ct)
    {
        foreach (RemoteConnectionBase connection in _connections.Values)
        {
            if (connection is NonPersistentTcpClientConnection) continue;
            if (connection is TcpServerConnection && !connection.Info.AlwaysConnected) continue;
            await _status.ReportConnectionAsync(connection.Info.Name, connection.IsConnected, ct).ConfigureAwait(false);
        }
    }

    private sealed class RemoteNode(string nodeName, IReadOnlyList<RemoteConnectionBase> connections) : IRemoteNode
    {
        public string NodeName { get; } = nodeName;

        public IReadOnlyList<IRemoteConnection> Connections { get; } = connections;

        public bool IsConnected => connections.Any(c => c.IsConnected);

        public IRemoteTcpConnection Tcp => connections.OfType<IRemoteTcpConnection>().FirstOrDefault()
            ?? throw new RemoteUnavailableException($"Node {NodeName} tidak memiliki koneksi TCP.");

        public IRemoteHttpClient Http => connections.OfType<IRemoteHttpClient>().FirstOrDefault()
            ?? throw new RemoteUnavailableException($"Node {NodeName} tidak memiliki koneksi HTTP klien.");

        public IRemoteConnection GetConnection(string connectionName) =>
            connections.FirstOrDefault(c => c.Info.Name == connectionName)
            ?? throw new RemoteUnavailableException($"Koneksi {connectionName} tidak ditemukan di node {NodeName}.");
    }
}
