namespace SyncNetPro.Sdk.Tracing;

/// <summary>
/// Satu entri trace. Bentuk JSON-nya adalah kontrak Log Services (dok. 02 §5), identik dengan
/// <c>LogModel</c> SDK lama: <c>Datetime, AppName, FileName, LogType, Title, Detail</c>.
/// </summary>
public sealed class TraceRecord
{
    /// <summary>Tipe log info.</summary>
    public const string InfoType = "info";

    /// <summary>Tipe log transaksi.</summary>
    public const string TransactionType = "transaction";

    /// <summary>Waktu (lokal).</summary>
    public DateTime Datetime { get; set; }

    /// <summary>Nama aplikasi.</summary>
    public string? AppName { get; set; }

    /// <summary>Nama file log (biasanya nama node).</summary>
    public string? FileName { get; set; }

    /// <summary><c>info</c> atau <c>transaction</c>.</summary>
    public string? LogType { get; set; }

    /// <summary>Judul.</summary>
    public string? Title { get; set; }

    /// <summary>Isi.</summary>
    public object? Detail { get; set; }
}
