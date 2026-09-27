using System.Net.Sockets;
using System.Text;

namespace SyncNetApi.Services.Monitoring
{
    public class SwitchCommandClient : ISwitchCommandClient
    {
        // Matches legacy Parameter.MAX_WAIT_MSG (NbCommand waits up to 3s via a
        // ManualResetEvent covering connect+send+receive as a single deadline).
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

        // Matches legacy Parameter.MAX_LENGTH_MSG (XSocketClient) — a header claiming a bigger
        // response than this is treated the same way legacy silently ignores it.
        private const int MaxResponseBytes = 16384;

        public async Task<string> SendAsync(string host, int port, string command, CancellationToken ct = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(Timeout);

            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(host, port, cts.Token);

                using var stream = client.GetStream();

                // Legacy NbTcpHeader.AddTCPHeader: 2-byte big-endian length header, EXCLUDING
                // the header itself (XSocketClient.TCP_HEADER defaults to TCP_ExcludeHeader),
                // followed by the raw command bytes (NbConvert.StringToBytes maps each char to
                // a byte 1:1 — equivalent to Latin1 for the plain-ASCII commands this sends).
                var payload = Encoding.Latin1.GetBytes(command);
                var header = new byte[] { (byte)(payload.Length >> 8), (byte)(payload.Length & 0xFF) };

                await stream.WriteAsync(header, cts.Token);
                await stream.WriteAsync(payload, cts.Token);

                var responseHeader = new byte[2];
                await ReadExactAsync(stream, responseHeader, cts.Token);
                var responseLength = (responseHeader[0] << 8) | responseHeader[1];

                if (responseLength <= 0 || responseLength > MaxResponseBytes)
                    return string.Empty;

                var responseBody = new byte[responseLength];
                await ReadExactAsync(stream, responseBody, cts.Token);

                return Encoding.Latin1.GetString(responseBody);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static async Task ReadExactAsync(NetworkStream stream, byte[] buffer, CancellationToken ct)
        {
            var offset = 0;
            while (offset < buffer.Length)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), ct);
                if (read == 0) throw new IOException("Connection closed before the full response was received.");
                offset += read;
            }
        }
    }
}
