using SyncNetPro.Sdk.Nodes;

namespace SyncNetPro.Sdk.Remote;

/// <summary>Koneksi ke sistem eksternal (satu baris <c>sw_connections</c>).</summary>
public interface IRemoteConnection
{
    /// <summary>Konfigurasi koneksi.</summary>
    RemoteConnectionInfo Info { get; }

    /// <summary>
    /// Status koneksi. HTTP dan TCP non-persistent selalu dianggap siap (<c>true</c>);
    /// TCP server persistent: ada klien terkoneksi; TCP klien persistent: socket terkoneksi.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>Alamat lawan untuk trace (setara <c>NodeRemote.GetEndPointClient</c>).</summary>
    string? RemoteAddress { get; }
}

/// <summary>Koneksi TCP (server atau klien, persistent atau non-persistent).</summary>
public interface IRemoteTcpConnection : IRemoteConnection
{
    /// <summary>
    /// Mengirim pesan tanpa menunggu balasan. Balasan (bila ada) diteruskan ke
    /// <see cref="SyncNetInterface.OnRemoteMessageAsync"/>. Pada peran server dikirim ke klien pertama yang terkoneksi.
    /// </summary>
    /// <exception cref="RemoteUnavailableException">Tidak ada koneksi.</exception>
    Task SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mengirim pesan dan menunggu balasan yang kuncinya (<see cref="SyncNetInterface.GetRemoteCorrelationKey"/>)
    /// sama dengan <paramref name="correlationKey"/>. Pada TCP non-persistent, balasan pertama pada koneksi tersebut
    /// yang diambil. Timeout default = <c>request_timeout</c> node.
    /// </summary>
    /// <exception cref="RemoteUnavailableException">Tidak ada koneksi.</exception>
    /// <exception cref="TimeoutException">Tidak ada balasan dalam batas waktu.</exception>
    Task<byte[]> SendAndReceiveAsync(ReadOnlyMemory<byte> payload, string correlationKey, TimeSpan? timeout = null, CancellationToken cancellationToken = default);

    /// <summary>Menutup lalu membuka ulang socket (setara <c>ResetTcp</c>).</summary>
    Task ResetAsync(CancellationToken cancellationToken = default);
}

/// <summary>Koneksi HTTP klien ke sistem eksternal. Base URL = <c>ws_url</c>.</summary>
public interface IRemoteHttpClient : IRemoteConnection
{
    /// <summary>Mengirim request. Tidak ada retry otomatis (aman untuk transaksi keuangan).</summary>
    /// <exception cref="RemoteUnavailableException">Gagal terhubung/timeout transport.</exception>
    Task<RemoteHttpResponse> SendAsync(RemoteHttpRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Seluruh koneksi eksternal milik sebuah node.</summary>
public interface IRemoteNode
{
    /// <summary>Nama node.</summary>
    string NodeName { get; }

    /// <summary>Koneksi milik node (urutan sesuai konfigurasi).</summary>
    IReadOnlyList<IRemoteConnection> Connections { get; }

    /// <summary>Minimal satu koneksi siap.</summary>
    bool IsConnected { get; }

    /// <summary>Koneksi TCP pertama node.</summary>
    /// <exception cref="RemoteUnavailableException">Node tidak memiliki koneksi TCP.</exception>
    IRemoteTcpConnection Tcp { get; }

    /// <summary>Koneksi HTTP klien pertama node.</summary>
    /// <exception cref="RemoteUnavailableException">Node tidak memiliki koneksi HTTP klien.</exception>
    IRemoteHttpClient Http { get; }

    /// <summary>Koneksi berdasarkan nama (<c>conn_name</c>).</summary>
    /// <exception cref="RemoteUnavailableException">Koneksi tidak ditemukan.</exception>
    IRemoteConnection GetConnection(string connectionName);

    /// <summary>Singkatan <c>Tcp.SendAsync</c>.</summary>
    Task SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default) => Tcp.SendAsync(payload, cancellationToken);

    /// <summary>Singkatan <c>Tcp.SendAndReceiveAsync</c>.</summary>
    Task<byte[]> SendAndReceiveAsync(ReadOnlyMemory<byte> payload, string correlationKey, TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        Tcp.SendAndReceiveAsync(payload, correlationKey, timeout, cancellationToken);
}

/// <summary>Akses koneksi eksternal per node.</summary>
public interface IRemoteRegistry
{
    /// <summary>Koneksi eksternal milik node (kosong bila node tidak memiliki koneksi).</summary>
    IRemoteNode GetNode(string nodeName);
}

/// <summary>Koneksi ke sistem eksternal tidak tersedia.</summary>
public sealed class RemoteUnavailableException : Exception
{
    /// <summary>Membuat exception.</summary>
    public RemoteUnavailableException()
    {
    }

    /// <summary>Membuat exception dengan pesan.</summary>
    public RemoteUnavailableException(string message)
        : base(message)
    {
    }

    /// <summary>Membuat exception dengan pesan dan penyebab.</summary>
    public RemoteUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
