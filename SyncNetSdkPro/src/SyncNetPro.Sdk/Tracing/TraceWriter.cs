using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace SyncNetPro.Sdk.Tracing;

/// <summary>Antrean trace in-memory; dikirim oleh <see cref="TraceDispatcher"/>.</summary>
public sealed class TraceWriter : ITraceWriter
{
    private readonly Channel<TraceRecord> _queue;
    private readonly TimeProvider _time;
    private readonly string _appName;
    private volatile bool _enabled;
    private long _dropped;

    /// <summary>Membuat penulis trace.</summary>
    public TraceWriter(IOptions<SyncNetOptions> options, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(options);
        _time = time;
        _appName = options.Value.AppName;
        _enabled = options.Value.Trace.Enabled && options.Value.Trace.Sink != TraceSinkKind.None;
        _queue = Channel.CreateBounded<TraceRecord>(new BoundedChannelOptions(options.Value.Trace.QueueCapacity)
        {
            // Wait: TryWrite mengembalikan false saat penuh sehingga trace yang dibuang bisa dihitung.
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
        });
    }

    /// <inheritdoc />
    public bool IsEnabled => _enabled;

    /// <summary>Jumlah trace yang dibuang karena antrean penuh.</summary>
    public long DroppedCount => Interlocked.Read(ref _dropped);

    internal ChannelReader<TraceRecord> Reader => _queue.Reader;

    /// <inheritdoc />
    public void SetEnabled(bool enabled) => _enabled = enabled;

    /// <inheritdoc />
    public int Clear()
    {
        int count = 0;
        while (_queue.Reader.TryRead(out _)) count++;
        return count;
    }

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

        // Tidak mencatat ke ILogger di sini: logger file juga meneruskan ke trace (hindari rekursi).
        if (!_queue.Writer.TryWrite(record)) Interlocked.Increment(ref _dropped);
    }
}
