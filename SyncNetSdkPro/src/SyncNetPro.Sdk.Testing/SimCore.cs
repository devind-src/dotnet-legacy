using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SyncNetPro.Contracts;
using SyncNetPro.Sdk.Core;
using SyncNetPro.Sdk.Nodes;
using SyncNetPro.Sdk.Transport;

namespace SyncNetPro.Sdk.Testing;

/// <summary>
/// Simulator SyncNet Core. Meniru perilaku Core yang terlihat oleh interface (dok. 02 §3):
/// kanal sink/source dengan framing 2 byte + JSON, korelasi <c>tran_type+datetime_tran+trace_number+terminal_id</c>,
/// <c>94 Duplicate transaction</c>, <c>91 Link down</c>, timeout (+ auto-reversal opsional), respons terlambat,
/// serta validasi kontrak untuk setiap pesan dari interface.
/// </summary>
public sealed class SimCore : IAsyncDisposable
{
    private readonly SimCoreOptions _options;
    private readonly ILogger _logger;
    private readonly CoreMessageCodec _codec = CoreMessageCodec.Default;
    private readonly CoreSwitchKeyProvider _keys = new();
    private readonly Dictionary<string, SimNode> _nodes = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, TaskCompletionSource<(CoreResponse Response, IReadOnlyList<string> Warnings)>> _pending = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<SimMessage> _messages = new();
    private readonly ConcurrentQueue<SimTrace> _traces = new();
    private readonly List<IAsyncDisposable> _stubs = [];
    private HsmStub? _hsm;
    private TcpFrameServer? _logServices;
    private long _sequence;

    private SimCore(SimCoreOptions options, ILogger logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>Dipanggil setiap ada pesan baru (untuk UI/SSE).</summary>
    public event Action<SimMessage>? MessageLogged;

    /// <summary>Dipanggil setiap trace diterima Log Services tiruan.</summary>
    public event Action<SimTrace>? TraceReceived;

    /// <summary>Konfigurasi.</summary>
    public SimCoreOptions Options => _options;

    /// <summary>Node yang disimulasikan.</summary>
    public IReadOnlyCollection<SimNode> Nodes => _nodes.Values;

    /// <summary>Riwayat pesan (terbaru di akhir).</summary>
    public IReadOnlyList<SimMessage> Messages => [.. _messages];

    /// <summary>Trace yang diterima.</summary>
    public IReadOnlyList<SimTrace> Traces => [.. _traces];

    /// <summary>Semua peringatan kontrak yang ditemukan.</summary>
    public IReadOnlyList<string> ContractWarnings => [.. _messages.SelectMany(m => m.Warnings.Select(w => $"{m.Node}/{m.Channel}: {w}"))];

    /// <summary>Port Log Services tiruan (bila aktif).</summary>
    public int? LogServicesPort => _logServices?.LocalEndPoint.Port;

    /// <summary>Stub sistem eksternal yang berjalan.</summary>
    public IReadOnlyList<RemoteStub> RemoteStubs => [.. _stubs.OfType<RemoteStub>()];

    /// <summary>URL HSM tiruan (null bila <see cref="SimCoreOptions.Hsm"/> tidak aktif).</summary>
    public string? HsmUrl => _hsm is null ? null : $"http://{_options.BindAddress}:{_hsm.Port}";

    /// <summary>Request yang diterima HSM tiruan.</summary>
    public IReadOnlyList<SimHsmRequest> HsmRequests => _hsm is null ? [] : [.. _hsm.Requests];

    /// <summary>Menjalankan SimCore.</summary>
    public static async Task<SimCore> StartAsync(SimCoreOptions options, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        var core = new SimCore(options, logger ?? NullLogger.Instance);
        try
        {
            await core.StartListenersAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await core.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        return core;
    }

    /// <summary>Node berdasarkan nama.</summary>
    /// <exception cref="KeyNotFoundException">Node tidak ada.</exception>
    public SimNode Node(string name) =>
        _nodes.TryGetValue(name, out SimNode? node) ? node : throw new KeyNotFoundException($"Node {name} tidak ada di SimCore.");

    /// <summary>Menunggu kanal node terkoneksi (sink dan/atau source sesuai kategori).</summary>
    public async Task WaitForConnectionAsync(string node, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        SimNode n = Node(node);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);
        try
        {
            while (!n.IsReady) await Task.Delay(20, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Interface belum terkoneksi ke node {node} dalam {timeout.TotalSeconds:0} detik " +
                $"(sink {(n.SinkConnected ? "OK" : $"menunggu di port {n.SinkPort}")}, source {(n.SourceConnected ? "OK" : $"menunggu di port {n.SourcePort}")}).");
        }
    }

    /// <summary>
    /// Core mengirim request ke interface (kanal sink) dan menunggu balasan — perilaku sama dengan Core SinkNode.
    /// </summary>
    public async Task<SimResult> SendAsync(string node, CoreRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        SimNode n = Node(node);
        var watch = Stopwatch.StartNew();

        if (n.Sink is null || !n.SinkConnected)
        {
            CoreResponse linkDown = request.ToResponse("91", "Link down", AuthorizedBy.Internal);
            Log(n.Name, SimChannel.Sink, SimDirection.ToInterface, Serialize(request), [], "link down: Core membalas 91 tanpa mengirim");
            return new SimResult(SimOutcome.LinkDown, linkDown, watch.Elapsed, []);
        }

        string key = PendingKey(n.Name, _keys.GetKey(request));
        var completion = new TaskCompletionSource<(CoreResponse, IReadOnlyList<string>)>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pending.TryAdd(key, completion))
        {
            CoreResponse duplicate = request.ToResponse("94", "Duplicate transaction", AuthorizedBy.Internal);
            Log(n.Name, SimChannel.Sink, SimDirection.ToInterface, Serialize(request), [], "duplikat: Core membalas 94 tanpa mengirim");
            return new SimResult(SimOutcome.Duplicate, duplicate, watch.Elapsed, []);
        }

        bool isAdvice = request.TranType is TranType.Advice or TranType.Reversal;
        TimeSpan wait = timeout ?? TimeSpan.FromSeconds(isAdvice ? n.Options.AdviceTimeoutSeconds : n.Options.RequestTimeoutSeconds);
        try
        {
            string json = Serialize(request);
            FramedConnection connection = n.Sink.Connections.FirstOrDefault(c => c.IsOpen)
                ?? throw new InvalidOperationException("Kanal sink terputus.");
            await connection.SendAsync(Encoding.UTF8.GetBytes(json), cancellationToken).ConfigureAwait(false);
            Log(n.Name, SimChannel.Sink, SimDirection.ToInterface, json, []);

            (CoreResponse response, IReadOnlyList<string> warnings) = await completion.Task.WaitAsync(wait, cancellationToken).ConfigureAwait(false);
            return new SimResult(SimOutcome.Responded, response, watch.Elapsed, warnings);
        }
        catch (TimeoutException)
        {
            _pending.TryRemove(new KeyValuePair<string, TaskCompletionSource<(CoreResponse, IReadOnlyList<string>)>>(key, completion));
            Log(n.Name, SimChannel.Sink, SimDirection.ToInterface, Serialize(request), [], $"timeout {wait.TotalSeconds:0.#} detik");
            if (n.Options.AutoReversal && !isAdvice) _ = SendAutoReversalAsync(n.Name, request);
            return new SimResult(SimOutcome.Timeout, null, watch.Elapsed, []);
        }
        finally
        {
            _pending.TryRemove(new KeyValuePair<string, TaskCompletionSource<(CoreResponse, IReadOnlyList<string>)>>(key, completion));
        }
    }

    /// <summary>Mengirim command ke command port interface (<see cref="SimCoreOptions.Interface"/>).</summary>
    public Task<string> CommandAsync(string command, CancellationToken cancellationToken = default)
    {
        SimCommandTarget target = _options.Interface ?? throw new InvalidOperationException("SimCoreOptions.Interface (command port interface) belum diisi.");
        return SimCommandClient.SendAsync(target.Host, target.Port, command, TimeSpan.FromSeconds(10), cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (TaskCompletionSource<(CoreResponse, IReadOnlyList<string>)> pending in _pending.Values) pending.TrySetCanceled();
        foreach (SimNode node in _nodes.Values) await node.DisposeAsync().ConfigureAwait(false);
        if (_logServices is not null) await _logServices.DisposeAsync().ConfigureAwait(false);
        foreach (IAsyncDisposable stub in _stubs) await stub.DisposeAsync().ConfigureAwait(false);
    }

    private async Task StartListenersAsync(CancellationToken cancellationToken)
    {
        IPAddress bind = IPAddress.Parse(_options.BindAddress);
        foreach (SimNodeOptions options in _options.Nodes)
        {
            var node = new SimNode(options);
            _nodes[options.Name] = node;

            if (options.Category != NodeCategory.Merchant)
            {
                node.Sink = CreateServer(bind, options.PortOut, (_, payload) => OnSinkFrameAsync(node, payload), node, SimChannel.Sink);
                await node.Sink.StartAsync(cancellationToken).ConfigureAwait(false);
            }

            if (options.Category != NodeCategory.BillerIssuer)
            {
                node.Source = CreateServer(bind, options.PortIn, (connection, payload) => OnSourceFrameAsync(node, connection, payload), node, SimChannel.Source);
                await node.Source.StartAsync(cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("Node {Node}: sink port {Sink}, source port {Source}", node.Name, node.SinkPort, node.SourcePort);
        }

        if (_options.LogServicesPort is int logPort)
        {
            _logServices = new TcpFrameServer(new IPEndPoint(bind, logPort), LengthPrefixCodec.Default, new TcpFrameServerOptions(), _logger)
            {
                FrameReceived = (_, payload) =>
                {
                    var trace = new SimTrace(Interlocked.Increment(ref _sequence), DateTimeOffset.Now, Encoding.UTF8.GetString(payload));
                    Enqueue(_traces, trace);
                    TraceReceived?.Invoke(trace);
                    return Task.CompletedTask;
                },
            };
            await _logServices.StartAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Log Services tiruan di port {Port}", LogServicesPort);
        }

        foreach (RemoteStubOptions stub in _options.RemoteStubs)
        {
            _stubs.Add(await RemoteStub.StartAsync(stub, bind, _logger, cancellationToken).ConfigureAwait(false));
        }

        if (_options.Hsm is not null)
        {
            _hsm = await HsmStub.StartAsync(_options.Hsm, bind, _logger, cancellationToken).ConfigureAwait(false);
            _stubs.Add(_hsm);
        }
    }

    private TcpFrameServer CreateServer(IPAddress bind, int port, Func<FramedConnection, byte[], Task> onFrame, SimNode node, SimChannel channel) =>
        new(new IPEndPoint(bind, port), LengthPrefixCodec.Default, new TcpFrameServerOptions(), _logger)
        {
            FrameReceived = onFrame,
            Connected = c =>
            {
                Log(node.Name, channel, SimDirection.FromInterface, "{}", [], $"interface terkoneksi dari {c.RemoteEndPoint}");
                return Task.CompletedTask;
            },
            Disconnected = c =>
            {
                Log(node.Name, channel, SimDirection.FromInterface, "{}", [], $"interface terputus ({c.RemoteEndPoint})");
                return Task.CompletedTask;
            },
        };

    private Task OnSinkFrameAsync(SimNode node, byte[] payload)
    {
        string json = Encoding.UTF8.GetString(payload);
        if (!TryParse(json, out JObject? obj, out string? error))
        {
            Log(node.Name, SimChannel.Sink, SimDirection.FromInterface, json, [error!], "JSON tidak valid");
            return Task.CompletedTask;
        }

        IReadOnlyList<string> warnings = ContractValidator.ValidateResponse(obj);
        CoreResponse response = _codec.Serializer.Deserialize<CoreResponse>(json);
        string key = PendingKey(node.Name, _keys.GetKey(response));

        if (_pending.TryRemove(key, out TaskCompletionSource<(CoreResponse, IReadOnlyList<string>)>? completion))
        {
            Log(node.Name, SimChannel.Sink, SimDirection.FromInterface, json, warnings);
            completion.TrySetResult((response, warnings));
        }
        else
        {
            Log(node.Name, SimChannel.Sink, SimDirection.FromInterface, json, warnings,
                "tidak dikenal/terlambat: tidak ada request yang menunggu dengan tran_type+datetime_tran+trace_number+terminal_id ini");
        }

        return Task.CompletedTask;
    }

    private async Task OnSourceFrameAsync(SimNode node, FramedConnection connection, byte[] payload)
    {
        string json = Encoding.UTF8.GetString(payload);
        if (!TryParse(json, out JObject? obj, out string? error))
        {
            Log(node.Name, SimChannel.Source, SimDirection.FromInterface, json, [error!], "JSON tidak valid");
            return;
        }

        CoreRequest request = _codec.Serializer.Deserialize<CoreRequest>(json);
        Log(node.Name, SimChannel.Source, SimDirection.FromInterface, json, ContractValidator.ValidateRequest(obj));
        node.Received.Enqueue(request);

        (CoreResponse? response, int delay) = SourceResponder.Respond(node.Options.Responder, request);
        if (response is null) return;

        _ = Task.Run(async () =>
        {
            if (delay > 0) await Task.Delay(delay).ConfigureAwait(false);
            string reply = Serialize(response);
            try
            {
                await connection.SendAsync(Encoding.UTF8.GetBytes(reply)).ConfigureAwait(false);
                Log(node.Name, SimChannel.Source, SimDirection.ToInterface, reply, []);
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException)
            {
                Log(node.Name, SimChannel.Source, SimDirection.ToInterface, reply, [], $"gagal membalas: {ex.Message}");
            }
        });
    }

    private async Task SendAutoReversalAsync(string node, CoreRequest original)
    {
        // Setara Core: original_data = TRAN_TYPE + datetime_tran + trace_number; transaksi dikirim ulang sebagai REV.
        CoreRequest reversal = _codec.Serializer.Deserialize<CoreRequest>(Serialize(original));
        reversal.MessageType = "0400";
        reversal.TranType = TranType.Reversal;
        reversal.OriginalData = (original.TranType ?? string.Empty).ToUpperInvariant() + original.TransactionDateTime + original.TraceNumber;
        Log(node, SimChannel.Sink, SimDirection.ToInterface, Serialize(reversal), [], "auto-reversal (auto_reversal node aktif)");
        await SendAsync(node, reversal).ConfigureAwait(false);
    }

    private static bool TryParse(string json, out JObject obj, out string? error)
    {
        try
        {
            obj = JObject.Parse(json);
            error = null;
            return true;
        }
        catch (JsonException ex)
        {
            obj = [];
            error = $"JSON tidak valid: {ex.Message}";
            return false;
        }
    }

    private string Serialize<T>(T message) => _codec.Serializer.Serialize(message);

    private static string PendingKey(string node, string key) => string.Concat(node, "\n", key);

    private void Log(string node, SimChannel channel, SimDirection direction, string json, IReadOnlyList<string> warnings, string? note = null)
    {
        var message = new SimMessage(Interlocked.Increment(ref _sequence), DateTimeOffset.Now, node, channel, direction, json, warnings, note);
        Enqueue(_messages, message);
        foreach (string warning in warnings) _logger.LogWarning("[{Node}/{Channel}] {Warning}", node, channel, warning);
        MessageLogged?.Invoke(message);
    }

    private void Enqueue<T>(ConcurrentQueue<T> queue, T item)
    {
        queue.Enqueue(item);
        while (queue.Count > _options.HistoryLimit && queue.TryDequeue(out _))
        {
        }
    }
}

/// <summary>Status satu node SimCore.</summary>
public sealed class SimNode : IAsyncDisposable
{
    internal SimNode(SimNodeOptions options) => Options = options;

    /// <summary>Konfigurasi.</summary>
    public SimNodeOptions Options { get; }

    /// <summary>Nama.</summary>
    public string Name => Options.Name;

    internal TcpFrameServer? Sink { get; set; }

    internal TcpFrameServer? Source { get; set; }

    /// <summary>Request yang diterima dari interface pada kanal source.</summary>
    public ConcurrentQueue<CoreRequest> Received { get; } = new();

    /// <summary>Port sink aktual (0 bila tidak ada).</summary>
    public int SinkPort => Sink?.LocalEndPoint.Port ?? 0;

    /// <summary>Port source aktual (0 bila tidak ada).</summary>
    public int SourcePort => Source?.LocalEndPoint.Port ?? 0;

    /// <summary>Interface terkoneksi ke kanal sink.</summary>
    public bool SinkConnected => Sink?.HasConnections == true;

    /// <summary>Interface terkoneksi ke kanal source.</summary>
    public bool SourceConnected => Source?.HasConnections == true;

    /// <summary>Semua kanal sesuai kategori sudah terkoneksi.</summary>
    public bool IsReady => (Sink is null || SinkConnected) && (Source is null || SourceConnected);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Sink is not null) await Sink.DisposeAsync().ConfigureAwait(false);
        if (Source is not null) await Source.DisposeAsync().ConfigureAwait(false);
    }
}
