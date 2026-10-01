using System.Net.Sockets;
using SyncNetPro.Sdk.Transport;

namespace SyncNetPro.Sdk.Testing;

/// <summary>
/// Klien TCP sederhana untuk menguji interface yang berperan sebagai server (mis. inbound ISO dari bank/EDC):
/// kirim satu pesan ber-header lalu tunggu satu balasan.
/// </summary>
public static class SimTcpClient
{
    /// <summary>Kirim <paramref name="payload"/> dan kembalikan balasan pertama (tanpa header).</summary>
    /// <param name="host">Host interface.</param>
    /// <param name="port">Port koneksi server interface.</param>
    /// <param name="payload">Pesan (tanpa header).</param>
    /// <param name="header">Header TCP; <c>null</c> = 2 byte biner exclude (default SDK lama).</param>
    /// <param name="timeout">Batas waktu tunggu balasan.</param>
    /// <param name="cancellationToken">Pembatalan.</param>
    /// <exception cref="TimeoutException">Tidak ada balasan.</exception>
    public static async Task<byte[]> SendAsync(string host, int port, ReadOnlyMemory<byte> payload, TcpHeaderOptions? header = null, TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var codec = new LengthPrefixCodec(header ?? TcpHeaderOptions.Default);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout ?? TimeSpan.FromSeconds(30));
        try
        {
            using var tcp = new TcpClient();
            await tcp.ConnectAsync(host, port, cts.Token).ConfigureAwait(false);
            NetworkStream stream = tcp.GetStream();
            await stream.WriteAsync(codec.Encode(payload.Span), cts.Token).ConfigureAwait(false);

            byte[] head = new byte[codec.HeaderLength];
            await stream.ReadExactlyAsync(head, cts.Token).ConfigureAwait(false);
            byte[] reply = new byte[codec.ReadPayloadLength(head)];
            await stream.ReadExactlyAsync(reply, cts.Token).ConfigureAwait(false);
            return reply;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Tidak ada balasan dari {host}:{port}.");
        }
    }
}
