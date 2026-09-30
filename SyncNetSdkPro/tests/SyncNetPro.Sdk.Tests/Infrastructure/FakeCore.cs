using System.Collections.Concurrent;
using System.Net;
using System.Threading.Channels;
using Microsoft.Extensions.Logging.Abstractions;
using SyncNetPro.Contracts;
using SyncNetPro.Sdk.Transport;

namespace SyncNetPro.Sdk.Tests.Infrastructure;

/// <summary>
/// Core tiruan minimal untuk test (cikal bakal SimCore fase 4): listener sink (<c>port_out</c>) dan
/// source (<c>port_in</c>) dengan framing 2 byte big-endian + JSON, seperti Core sebenarnya.
/// </summary>
internal sealed class FakeCore : IAsyncDisposable
{
    private readonly TcpFrameServer _sink;
    private readonly TcpFrameServer _source;
    private readonly CoreMessageCodec _codec = CoreMessageCodec.Default;

    private FakeCore()
    {
        _sink = new TcpFrameServer(new IPEndPoint(IPAddress.Loopback, 0), LengthPrefixCodec.Default, new TcpFrameServerOptions(), NullLogger.Instance)
        {
            FrameReceived = (_, payload) => SinkResponses.Writer.WriteAsync(_codec.DecodePayload<CoreResponse>(payload)).AsTask(),
            Connected = _ => { Interlocked.Increment(ref SinkConnects); return Task.CompletedTask; },
            Disconnected = _ => { Interlocked.Increment(ref SinkDisconnects); return Task.CompletedTask; },
        };
        _source = new TcpFrameServer(new IPEndPoint(IPAddress.Loopback, 0), LengthPrefixCodec.Default, new TcpFrameServerOptions(), NullLogger.Instance)
        {
            FrameReceived = async (connection, payload) =>
            {
                CoreRequest request = _codec.DecodePayload<CoreRequest>(payload);
                SourceRequests.Enqueue(request);
                CoreResponse? response = SourceResponder?.Invoke(request);
                if (response is not null) await connection.SendAsync(_codec.Serializer.SerializeToUtf8Bytes(response));
            },
            Connected = _ => { Interlocked.Increment(ref SourceConnects); return Task.CompletedTask; },
            Disconnected = _ => { Interlocked.Increment(ref SourceDisconnects); return Task.CompletedTask; },
        };
    }

    public int SinkConnects;
    public int SinkDisconnects;
    public int SourceConnects;
    public int SourceDisconnects;

    /// <summary>Response yang dibalas interface pada kanal sink.</summary>
    public Channel<CoreResponse> SinkResponses { get; } = Channel.CreateUnbounded<CoreResponse>();

    /// <summary>Request yang dikirim interface pada kanal source.</summary>
    public ConcurrentQueue<CoreRequest> SourceRequests { get; } = new();

    /// <summary>Penjawab otomatis kanal source (null = tidak membalas).</summary>
    public Func<CoreRequest, CoreResponse?>? SourceResponder { get; set; } = request => request.ToResponse("00", "Approved");

    public int SinkPort => _sink.LocalEndPoint.Port;

    public int SourcePort => _source.LocalEndPoint.Port;

    public bool SinkConnected => _sink.HasConnections;

    public bool SourceConnected => _source.HasConnections;

    public static async Task<FakeCore> StartAsync()
    {
        var core = new FakeCore();
        await core._sink.StartAsync();
        await core._source.StartAsync();
        return core;
    }

    /// <summary>Core mengirim request ke interface (kanal sink).</summary>
    public async Task SendToInterfaceAsync(CoreRequest request)
    {
        await Wait.UntilAsync(() => _sink.Connections.Any(c => c.IsOpen), "koneksi sink diterima FakeCore");
        FramedConnection connection = _sink.Connections.First(c => c.IsOpen);
        await connection.SendAsync(_codec.Serializer.SerializeToUtf8Bytes(request));
    }

    /// <summary>Core mengirim response "spontan" ke interface (kanal source).</summary>
    public async Task SendSourceResponseAsync(CoreResponse response)
    {
        await Wait.UntilAsync(() => _source.Connections.Any(c => c.IsOpen), "koneksi source diterima FakeCore");
        FramedConnection connection = _source.Connections.First(c => c.IsOpen);
        await connection.SendAsync(_codec.Serializer.SerializeToUtf8Bytes(response));
    }

    public async Task<CoreResponse> ReceiveResponseAsync(int timeoutMs = 5000)
    {
        using var cts = new CancellationTokenSource(timeoutMs);
        return await SinkResponses.Reader.ReadAsync(cts.Token);
    }

    public async ValueTask DisposeAsync()
    {
        await _sink.DisposeAsync();
        await _source.DisposeAsync();
    }
}
