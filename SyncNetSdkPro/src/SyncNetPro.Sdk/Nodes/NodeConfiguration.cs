using System.Net;

namespace SyncNetPro.Sdk.Nodes;

/// <summary>Snapshot konfigurasi aplikasi: node, koneksi, command port, Log Services.</summary>
/// <param name="Nodes">Node milik aplikasi.</param>
/// <param name="Connections">Koneksi eksternal milik node-node tersebut.</param>
/// <param name="CommandPort">Port command (<c>sw_app.command_port</c>).</param>
/// <param name="LogServices">Alamat Log Services (<c>sw_app</c> baris <c>Log Services</c>).</param>
public sealed record NodeConfiguration(
    IReadOnlyList<NodeInfo> Nodes,
    IReadOnlyList<RemoteConnectionInfo> Connections,
    int? CommandPort,
    DnsEndPoint? LogServices)
{
    /// <summary>Konfigurasi kosong.</summary>
    public static NodeConfiguration Empty { get; } = new([], [], null, null);
}

/// <summary>Sumber konfigurasi node.</summary>
public interface INodeConfigurationSource
{
    /// <summary>Membaca konfigurasi untuk aplikasi <paramref name="appName"/>.</summary>
    Task<NodeConfiguration> LoadAsync(string appName, CancellationToken cancellationToken);
}
