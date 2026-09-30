using System.Text;
using Microsoft.Extensions.Logging;
using SyncNetPro.Sdk.Transport;

namespace SyncNetPro.Sdk.Tracing;

/// <summary>
/// Trace ke Log Services via TCP (framing 2 byte biner big-endian). Host diambil dari konfigurasi
/// (<c>sw_app.host</c>), bukan selalu 127.0.0.1 (perbaikan B5).
/// </summary>
public sealed class LogServicesTcpSink : ITraceSink
{
    private readonly TcpFrameClient _client;

    /// <summary>Membuat sink.</summary>
    public LogServicesTcpSink(string host, int port, ILogger logger, TimeProvider? time = null)
    {
        _client = new TcpFrameClient(host, port, LengthPrefixCodec.Default,
            new TcpFrameClientOptions { ReconnectDelay = TimeSpan.FromSeconds(5) }, logger, time);
    }

    /// <inheritdoc />
    public string Name => $"Log Services ({_client.Target})";

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) => _client.StartAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<bool> TrySendAsync(TraceRecord record, string json, CancellationToken cancellationToken)
    {
        if (!_client.IsConnected) return false;
        try
        {
            await _client.SendAsync(Encoding.UTF8.GetBytes(json), cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (NotConnectedException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _client.DisposeAsync();
}
