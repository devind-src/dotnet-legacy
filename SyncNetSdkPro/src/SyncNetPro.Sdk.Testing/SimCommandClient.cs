using System.Net.Sockets;
using System.Text;
using SyncNetPro.Sdk.Transport;

namespace SyncNetPro.Sdk.Testing;

/// <summary>Klien command port interface (setara SyncNetWebApi MonitoringCommandService).</summary>
public static class SimCommandClient
{
    /// <summary>Mengirim command teks dan mengembalikan balasan.</summary>
    public static async Task<string> SendAsync(string host, int port, string command, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);
        using var tcp = new TcpClient();
        await tcp.ConnectAsync(host, port, cts.Token).ConfigureAwait(false);
        NetworkStream stream = tcp.GetStream();
        await stream.WriteAsync(LengthPrefixCodec.Default.Encode(Encoding.UTF8.GetBytes(command)), cts.Token).ConfigureAwait(false);

        byte[] header = new byte[2];
        await stream.ReadExactlyAsync(header, cts.Token).ConfigureAwait(false);
        byte[] payload = new byte[LengthPrefixCodec.Default.ReadPayloadLength(header)];
        await stream.ReadExactlyAsync(payload, cts.Token).ConfigureAwait(false);
        return Encoding.UTF8.GetString(payload);
    }
}
