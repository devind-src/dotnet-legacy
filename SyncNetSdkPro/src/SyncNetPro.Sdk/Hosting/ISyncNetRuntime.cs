using SyncNetPro.Sdk.Nodes;

namespace SyncNetPro.Sdk.Hosting;

/// <summary>Kontrol runtime interface.</summary>
public interface ISyncNetRuntime
{
    /// <summary>Nama aplikasi.</summary>
    string AppName { get; }

    /// <summary>Versi (dibalas command <c>VERSION</c>).</summary>
    string Version { get; }

    /// <summary>Muat ulang konfigurasi dan sesuaikan kanal (setara command <c>RESYNC</c>).</summary>
    Task<NodeConfiguration> ReloadAsync(CancellationToken cancellationToken = default);
}
