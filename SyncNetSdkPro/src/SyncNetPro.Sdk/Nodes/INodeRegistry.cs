using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace SyncNetPro.Sdk.Nodes;

/// <summary>Daftar node dan koneksi aktif (read-only; berubah hanya lewat <see cref="ReloadAsync"/>).</summary>
public interface INodeRegistry
{
    /// <summary>Snapshot konfigurasi terakhir.</summary>
    NodeConfiguration Current { get; }

    /// <summary>Node aktif.</summary>
    IReadOnlyList<NodeInfo> Nodes => Current.Nodes;

    /// <summary>Mencari node. Tidak pernah membuat objek baru (perbaikan B7).</summary>
    bool TryGetNode(string nodeName, [NotNullWhen(true)] out NodeInfo? node);

    /// <summary>Koneksi eksternal milik node.</summary>
    IReadOnlyList<RemoteConnectionInfo> GetConnections(string nodeName);

    /// <summary>Alamat Log Services.</summary>
    DnsEndPoint? LogServices => Current.LogServices;

    /// <summary>Membaca ulang konfigurasi dari sumbernya.</summary>
    Task<NodeConfiguration> ReloadAsync(CancellationToken cancellationToken);
}
