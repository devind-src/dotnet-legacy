namespace SyncNetPro.Sdk.Tracing;

/// <summary>Tujuan pengiriman trace.</summary>
public interface ITraceSink : IAsyncDisposable
{
    /// <summary>Nama sink untuk log.</summary>
    string Name { get; }

    /// <summary>Mulai (mis. membuka koneksi di background).</summary>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>Mengirim satu trace; <c>false</c> bila gagal (akan dialihkan ke file).</summary>
    Task<bool> TrySendAsync(TraceRecord record, string json, CancellationToken cancellationToken);
}
