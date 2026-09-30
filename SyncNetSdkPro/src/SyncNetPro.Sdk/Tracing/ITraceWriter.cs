namespace SyncNetPro.Sdk.Tracing;

/// <summary>Arah pesan pada trace.</summary>
public enum TraceDirection
{
    /// <summary>Pesan diterima dari pihak lain (<c>Message from</c>).</summary>
    Incoming,

    /// <summary>Pesan dikirim ke pihak lain (<c>Message to</c>).</summary>
    Outgoing,
}

/// <summary>
/// Penulis trace ke Log Services. Semua method otomatis tidak melakukan apa pun saat trace OFF,
/// sehingga pemanggil tidak perlu memeriksa <see cref="IsEnabled"/> (berbeda dengan SDK lama).
/// </summary>
public interface ITraceWriter
{
    /// <summary>Status trace (command <c>TRACE ON/OFF</c>).</summary>
    bool IsEnabled { get; }

    /// <summary>Mengaktifkan/menonaktifkan trace.</summary>
    void SetEnabled(bool enabled);

    /// <summary>Membuang trace yang masih antre (command <c>TRACE CLEAR</c>). Mengembalikan jumlah yang dibuang.</summary>
    int Clear();

    /// <summary>
    /// Trace pesan transaksi. Judul log: <c>&lt;{title}&gt; Message from|to {nodeName} {remoteAddress}</c>
    /// (format SDK lama), <c>LogType = "transaction"</c>.
    /// </summary>
    void Message(string nodeName, TraceDirection direction, string title, string content, string? remoteAddress = null);

    /// <summary>Trace informasi (<c>LogType = "info"</c>).</summary>
    void Info(string nodeName, string title, string? detail = null);
}
