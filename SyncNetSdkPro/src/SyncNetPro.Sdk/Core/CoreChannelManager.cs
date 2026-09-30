using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SyncNetPro.Contracts;
using SyncNetPro.Sdk.Logging;
using SyncNetPro.Sdk.Nodes;
using SyncNetPro.Sdk.Transport;

namespace SyncNetPro.Sdk.Core;

/// <summary>
/// Mengelola kanal TCP ke Core per node dan arah: outbound (Core SinkNode, <c>port_out</c>) dan
/// inbound (Core SourceNode, <c>port_in</c>). Semua kanal disimpan di satu tempat sehingga seluruhnya
/// ditutup saat berhenti/RESYNC (perbaikan B3).
/// </summary>
public sealed class CoreChannelManager : ICoreClient, IAsyncDisposable
{
    private readonly SyncNetOptions _options;
    private readonly SyncNetInterface _handler;
    private readonly INodeRegistry _registry;
    private readonly INodeStatusReporter _status;
    private readonly ICoreCorrelationKeyProvider _keys;
    private readonly SyncNetServices _services;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly TimeProvider _time;
    private readonly CoreMessageCodec _codec = CoreMessageCodec.Default;
    private readonly SemaphoreSlim _reconcileLock = new(1, 1);
    private readonly ConcurrentDictionary<(string Node, CoreChannelDirection Direction), CoreChannel> _channels = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _limits = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, TaskCompletionSource<CoreResponse>> _pending = new(StringComparer.Ordinal);
    private readonly CancellationTokenSource _stopping = new();
    private int _inFlight;

    /// <summary>Membuat manager.</summary>
    public CoreChannelManager(
        IOptions<SyncNetOptions> options,
        SyncNetInterface handler,
        INodeRegistry registry,
        INodeStatusReporter status,
        ICoreCorrelationKeyProvider keys,
        SyncNetServices services,
        ILoggerFactory loggerFactory,
        TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _handler = handler;
        _registry = registry;
        _status = status;
        _keys = keys;
        _services = services;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<CoreChannelManager>();
        _time = time;
    }

    /// <summary>Jumlah request Core yang sedang diproses handler.</summary>
    public int InFlight => Volatile.Read(ref _inFlight);

    /// <summary>Jumlah request inbound yang menunggu respons Core.</summary>
    public int PendingResponses => _pending.Count;

    /// <summary>Status seluruh kanal: (node, arah, terkoneksi, target).</summary>
    public IReadOnlyList<(string Node, CoreChannelDirection Direction, bool Connected, string Target)> Channels =>
        [.. _channels.Values.Select(c => (c.Node.Name, c.Direction, c.Client.IsConnected, c.Client.Target))];

    /// <inheritdoc />
    public bool IsConnected(string nodeName, CoreChannelDirection direction) =>
        _channels.TryGetValue((nodeName, direction), out CoreChannel? channel) && channel.Client.IsConnected;

    /// <summary>Menyesuaikan kanal dengan konfigurasi: buka node baru, tutup node yang dihapus, restart bila port berubah.</summary>
    public async Task ReconcileAsync(NodeConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        await _reconcileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var wanted = new Dictionary<(string, CoreChannelDirection), (NodeInfo Node, int Port)>();
            foreach (NodeInfo node in configuration.Nodes)
            {
                if (node.HasInbound) wanted[(node.Name, CoreChannelDirection.Inbound)] = (node, node.PortIn);
                if (node.HasOutbound) wanted[(node.Name, CoreChannelDirection.Outbound)] = (node, node.PortOut);
            }

            foreach (((string, CoreChannelDirection) key, CoreChannel channel) in _channels.ToArray())
            {
                if (!wanted.TryGetValue(key, out var target) || target.Port != channel.Port)
                {
                    _channels.TryRemove(key, out _);
                    _logger.LogInformation("Menutup kanal {Direction} {Node} ({Target})", channel.Direction, channel.Node.Name, channel.Client.Target);
                    await channel.DisposeAsync().ConfigureAwait(false);
                    await _status.ReportCoreChannelAsync(channel.Node.Name, channel.Direction, false, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    channel.Node = target.Node;
                }
            }

            foreach (((string, CoreChannelDirection) key, (NodeInfo node, int port)) in wanted)
            {
                if (_channels.ContainsKey(key)) continue;
                if (port <= 0)
                {
                    _logger.LogWarning("Node {Node}: port {Direction} tidak valid ({Port}); kanal tidak dibuka", node.Name, key.Item2, port);
                    continue;
                }

                CoreChannel channel = CreateChannel(node, key.Item2, port);
                _channels[key] = channel;
                await channel.Client.StartAsync(_stopping.Token).ConfigureAwait(false);
            }
        }
        finally
        {
            _reconcileLock.Release();
        }
    }

    /// <summary>
    /// Berhenti: tunggu request yang sedang diproses (maks. <paramref name="drainTimeout"/>), lalu tutup semua kanal.
    /// </summary>
    public async Task StopAsync(TimeSpan drainTimeout)
    {
        long deadline = _time.GetTimestamp();
        while (InFlight > 0 && _time.GetElapsedTime(deadline) < drainTimeout)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(50), _time).ConfigureAwait(false);
        }

        if (InFlight > 0) _logger.LogWarning("{Count} request Core masih diproses saat berhenti", InFlight);

        await _stopping.CancelAsync().ConfigureAwait(false);

        foreach (TaskCompletionSource<CoreResponse> pending in _pending.Values)
        {
            pending.TrySetException(new CoreUnavailableException("Interface sedang berhenti."));
        }

        foreach (CoreChannel channel in _channels.Values)
        {
            await channel.DisposeAsync().ConfigureAwait(false);
            await _status.ReportCoreChannelAsync(channel.Node.Name, channel.Direction, false).ConfigureAwait(false);
        }

        _channels.Clear();
    }

    /// <inheritdoc />
    public async Task<CoreResponse> SendAsync(string nodeName, CoreRequest request, CoreSendOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_registry.TryGetNode(nodeName, out NodeInfo? node))
            throw new CoreUnavailableException($"Node {nodeName} tidak ditemukan.");
        if (!node.HasInbound)
            throw new CoreUnavailableException($"Node {nodeName} (kategori {node.Category}) tidak memiliki kanal inbound ke Core.");
        if (!_channels.TryGetValue((nodeName, CoreChannelDirection.Inbound), out CoreChannel? channel) || !channel.Client.IsConnected)
            throw new CoreUnavailableException($"Kanal inbound {nodeName} ke Core tidak terkoneksi.");

        if (options?.ConnectionName is not null || options?.RemoteAddress is not null)
        {
            request.PrivateData ??= new PrivateData();
            if (options.ConnectionName is not null) request.PrivateData.ConnectionName = options.ConnectionName;
            if (options.RemoteAddress is not null) request.PrivateData.IpExternal = options.RemoteAddress;
        }

        string key = PendingKey(nodeName, _keys.GetKey(request));
        var completion = new TaskCompletionSource<CoreResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pending.TryAdd(key, completion))
            throw new DuplicateCoreRequestException($"Request {request.TranType} trace {request.TraceNumber} terminal {request.TerminalId} masih menunggu respons Core.");

        try
        {
            try
            {
                await channel.Client.SendAsync(_codec.Serializer.SerializeToUtf8Bytes(request), cancellationToken).ConfigureAwait(false);
            }
            catch (NotConnectedException ex)
            {
                throw new CoreUnavailableException($"Gagal mengirim ke Core ({nodeName}): {ex.Message}", ex);
            }

            TimeSpan timeout = options?.Timeout ?? ResponseTimeout(node, request.TranType);
            try
            {
                return await completion.Task.WaitAsync(timeout, _time, cancellationToken).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                throw new TimeoutException($"Core tidak membalas {request.TranType} trace {request.TraceNumber} dalam {timeout.TotalSeconds:0} detik ({nodeName}).");
            }
        }
        finally
        {
            _pending.TryRemove(new KeyValuePair<string, TaskCompletionSource<CoreResponse>>(key, completion));
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_stopping.IsCancellationRequested) await StopAsync(TimeSpan.Zero).ConfigureAwait(false);
        _stopping.Dispose();
        _reconcileLock.Dispose();
        foreach (SemaphoreSlim limit in _limits.Values) limit.Dispose();
    }

    private TimeSpan ResponseTimeout(NodeInfo node, string? tranType)
    {
        int seconds = tranType is TranType.Advice or TranType.Reversal ? node.AdviceTimeoutSeconds : node.RequestTimeoutSeconds;
        return TimeSpan.FromSeconds(seconds > 0 ? seconds : 30) + _options.Core.ResponseTimeoutMargin;
    }

    private static string PendingKey(string nodeName, string key) => string.Concat(nodeName, "\n", key);

    private CoreChannel CreateChannel(NodeInfo node, CoreChannelDirection direction, int port)
    {
        ILogger logger = _loggerFactory.CreateLogger($"SyncNetPro.Sdk.Core.{direction}");
        var client = new TcpFrameClient(_options.Core.Host, port, LengthPrefixCodec.Default,
            new TcpFrameClientOptions { ReconnectDelay = _options.Core.ReconnectDelay }, logger, _time);
        var channel = new CoreChannel(node, direction, port, client);

        client.Connected = _ => _status.ReportCoreChannelAsync(channel.Node.Name, direction, true, _stopping.Token);
        client.Disconnected = _ => _status.ReportCoreChannelAsync(channel.Node.Name, direction, false, CancellationToken.None);
        client.FrameReceived = direction == CoreChannelDirection.Outbound
            ? payload => OnCoreRequestFrameAsync(channel, payload)
            : payload => OnCoreResponseFrameAsync(channel, payload);

        _logger.LogInformation("Kanal {Direction} {Node} → Core {Host}:{Port}", direction, node.Name, _options.Core.Host, port);
        return channel;
    }

    private async Task OnCoreRequestFrameAsync(CoreChannel channel, byte[] payload)
    {
        CoreRequest request;
        try
        {
            request = _codec.DecodePayload<CoreRequest>(payload);
        }
        catch (Exception ex) when (ex is Newtonsoft.Json.JsonException or DecoderFallbackException)
        {
            _logger.LogError("Request dari Core ({Node}) tidak valid: {Error} {Payload}", channel.Node.Name, ex.Message, Encoding.UTF8.GetString(payload));
            return;
        }

        NodeInfo node = channel.Node;
        SemaphoreSlim limit = _limits.GetOrAdd(node.Name, _ => new SemaphoreSlim(_options.MaxConcurrentRequestsPerNode));
        await limit.WaitAsync(_stopping.Token).ConfigureAwait(false);
        Interlocked.Increment(ref _inFlight);

        _ = Task.Run(async () =>
        {
            ILogger logger = _loggerFactory.CreateLogger(_handler.GetType());
            using IDisposable? scope = logger.BeginNodeScope(node.Name);
            var context = new CoreRequestContext(node, request, (rsp, ct) => ReplyToCoreAsync(node.Name, rsp, ct), _services, logger);
            try
            {
                CoreResponse? response = await _handler.OnCoreRequestAsync(context, _stopping.Token).ConfigureAwait(false);
                if (response is not null && !context.HasReplied)
                {
                    await context.ReplyAsync(response, _stopping.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (_stopping.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Gagal memproses {TranType} trace {Trace} dari Core; tidak dibalas (Core menangani timeout)",
                    request.TranType, request.TraceNumber);
            }
            finally
            {
                Interlocked.Decrement(ref _inFlight);
                limit.Release();
            }
        });
    }

    private async Task ReplyToCoreAsync(string nodeName, CoreResponse response, CancellationToken cancellationToken)
    {
        if (!_channels.TryGetValue((nodeName, CoreChannelDirection.Outbound), out CoreChannel? channel))
            throw new CoreUnavailableException($"Kanal outbound {nodeName} tidak ada.");

        try
        {
            await channel.Client.SendAsync(_codec.Serializer.SerializeToUtf8Bytes(response), cancellationToken).ConfigureAwait(false);
        }
        catch (NotConnectedException ex)
        {
            throw new CoreUnavailableException($"Gagal membalas ke Core ({nodeName}): {ex.Message}", ex);
        }
    }

    private Task OnCoreResponseFrameAsync(CoreChannel channel, byte[] payload)
    {
        CoreResponse response;
        try
        {
            response = _codec.DecodePayload<CoreResponse>(payload);
        }
        catch (Exception ex) when (ex is Newtonsoft.Json.JsonException or DecoderFallbackException)
        {
            _logger.LogError("Respons dari Core ({Node}) tidak valid: {Error} {Payload}", channel.Node.Name, ex.Message, Encoding.UTF8.GetString(payload));
            return Task.CompletedTask;
        }

        NodeInfo node = channel.Node;
        if (_pending.TryRemove(PendingKey(node.Name, _keys.GetKey(response)), out TaskCompletionSource<CoreResponse>? completion))
        {
            completion.TrySetResult(response);
            return Task.CompletedTask;
        }

        _ = Task.Run(async () =>
        {
            ILogger logger = _loggerFactory.CreateLogger(_handler.GetType());
            using IDisposable? scope = logger.BeginNodeScope(node.Name);
            try
            {
                await _handler.OnUnmatchedCoreResponseAsync(new CoreResponseContext(node, response, _services, logger), _stopping.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Gagal memproses respons Core yang tidak dikenali");
            }
        });
        return Task.CompletedTask;
    }

    private sealed class CoreChannel(NodeInfo node, CoreChannelDirection direction, int port, TcpFrameClient client) : IAsyncDisposable
    {
        public NodeInfo Node { get; set; } = node;

        public CoreChannelDirection Direction { get; } = direction;

        public int Port { get; } = port;

        public TcpFrameClient Client { get; } = client;

        public ValueTask DisposeAsync() => Client.DisposeAsync();
    }
}
