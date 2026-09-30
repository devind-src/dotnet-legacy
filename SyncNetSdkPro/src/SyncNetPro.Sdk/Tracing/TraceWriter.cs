using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace SyncNetPro.Sdk.Tracing;

/// <summary>
/// Antrean trace in-memory; dikirim oleh <see cref="TraceDispatcher"/>. Trace transaksi adalah jejak audit:
/// bila antrean penuh, trace langsung ditulis ke file fallback (tidak pernah dibuang).
/// </summary>
public sealed class TraceWriter : ITraceWriter, IDisposable
{
    private readonly Channel<TraceRecord> _queue;
    private readonly TimeProvider _time;
    private readonly string _appName;
    private readonly FileTraceSink _overflow;
    private volatile bool _enabled;
    private long _overflowed;
    private long _lost;

    /// <summary>Membuat penulis trace dengan folder fallback dari lingkungan Core.</summary>
    public TraceWriter(IOptions<SyncNetOptions> options, TimeProvider time, Configuration.CoreEnvironment environment)
        : this(options, time, (environment ?? throw new ArgumentNullException(nameof(environment))).TraceDirectory)
    {
    }

    /// <summary>Membuat penulis trace dengan folder fallback tertentu.</summary>
    public TraceWriter(IOptions<SyncNetOptions> options, TimeProvider time, string overflowDirectory)
    {
        ArgumentNullException.ThrowIfNull(options);
        _overflow = new FileTraceSink(overflowDirectory, options.Value.Logging.LegacyWindowsFileNames);
        _time = time;
        _appName = options.Value.AppName;
        _enabled = options.Value.Trace.Enabled && options.Value.Trace.Sink != TraceSinkKind.None;
        _queue = Channel.CreateBounded<TraceRecord>(new BoundedChannelOptions(options.Value.Trace.QueueCapacity)
        {
            // Wait: TryWrite mengembalikan false saat penuh sehingga trace dapat dialihkan ke file.
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
        });
    }

    /// <inheritdoc />
    public bool IsEnabled => _enabled;

    /// <summary>Jumlah trace yang ditulis langsung ke file karena antrean penuh.</summary>
    public long OverflowCount => Interlocked.Read(ref _overflowed);

    /// <summary>Jumlah trace yang gagal ditulis ke mana pun (antrean penuh dan file tidak dapat ditulis).</summary>
    public long LostCount => Interlocked.Read(ref _lost);

    internal ChannelReader<TraceRecord> Reader => _queue.Reader;

    /// <inheritdoc />
    public void SetEnabled(bool enabled) => _enabled = enabled;

    /// <inheritdoc />
    public void Dispose() => _overflow.Dispose();

    /// <inheritdoc />
    public void Message(string nodeName, TraceDirection direction, string title, string content, string? remoteAddress = null)
    {
        string header = direction == TraceDirection.Incoming
            ? $"<{title}> Message from {nodeName} {remoteAddress}"
            : $"<{title}> Message to {nodeName} {remoteAddress}";
        Enqueue(nodeName, TraceRecord.TransactionType, header, content);
    }

    /// <inheritdoc />
    public void Info(string nodeName, string title, string? detail = null) =>
        Enqueue(string.IsNullOrEmpty(nodeName) ? _appName : nodeName, TraceRecord.InfoType, title, detail ?? string.Empty);

    private void Enqueue(string fileName, string logType, string title, string detail)
    {
        if (!_enabled) return;

        var record = new TraceRecord
        {
            Datetime = _time.GetLocalNow().LocalDateTime,
            AppName = _appName,
            FileName = fileName,
            LogType = logType,
            Title = title,
            Detail = detail,
        };

        if (_queue.Writer.TryWrite(record)) return;

        // Antrean penuh (mis. Log Services lambat/terputus): tulis langsung ke file fallback — jejak audit tidak dibuang.
        // Tidak mencatat ke ILogger di sini: logger file juga meneruskan ke trace (hindari rekursi).
        Interlocked.Increment(ref _overflowed);
        if (!_overflow.TryWrite(record)) Interlocked.Increment(ref _lost);
    }
}
