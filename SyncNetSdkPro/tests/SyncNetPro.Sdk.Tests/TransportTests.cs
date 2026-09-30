using System.Buffers;
using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using SyncNetPro.Sdk.Tests.Infrastructure;
using SyncNetPro.Sdk.Transport;

namespace SyncNetPro.Sdk.Tests;

public class LengthPrefixCodecTests
{
    public static TheoryData<string, string, string, int, string, int, bool> LegacyHeaders()
    {
        var data = new TheoryData<string, string, string, int, string, int, bool>();
        foreach (JToken c in JArray.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Golden", "tcpheader.json"))))
        {
            data.Add((string)c["type"]!, (string)c["mode"]!, (string)c["endian"]!, (int)c["length"]!, (string)c["header"]!, (int)c["decoded"]!, (bool)c["error"]!);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(LegacyHeaders))]
    public void Matches_legacy_TcpHeader(string type, string mode, string endian, int length, string header, int decoded, bool error)
    {
        var codec = new LengthPrefixCodec(new TcpHeaderOptions(
            Enum.Parse<TcpHeaderType>(type), Enum.Parse<TcpLengthMode>(mode), Enum.Parse<TcpEndianMode>(endian)));
        byte[] payload = Enumerable.Repeat((byte)0x41, length).ToArray();

        if (error)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => codec.Encode(payload));
            return;
        }

        byte[] frame = codec.Encode(payload);
        Assert.Equal(header, Convert.ToHexString(frame, 0, codec.HeaderLength));
        Assert.Equal(decoded, codec.ReadPayloadLength(frame));

        var buffer = new ReadOnlySequence<byte>(frame);
        Assert.True(codec.TryDecode(ref buffer, out byte[] roundTrip));
        Assert.Equal(payload, roundTrip);
        Assert.True(buffer.IsEmpty);
    }

    [Fact]
    public void Legacy_golden_covers_all_variants() => Assert.Equal(192, LegacyHeaders().Count);

    [Theory]
    [InlineData(TcpHeaderType.Bcd2Byte, new byte[] { 0x0A, 0x00 })]
    [InlineData(TcpHeaderType.Ascii4Digit, new byte[] { 0x30, 0x30, 0x3A, 0x30 })]
    [InlineData(TcpHeaderType.Binary2Byte, new byte[] { 0x00, 0x00 })]
    public void Rejects_invalid_header(TcpHeaderType type, byte[] header)
    {
        var codec = new LengthPrefixCodec(new TcpHeaderOptions(type));
        var buffer = new ReadOnlySequence<byte>([.. header, 1, 2, 3]);

        Assert.Throws<InvalidDataException>(() => codec.TryDecode(ref buffer, out _));
    }

    [Fact]
    public void Rejects_payload_above_limit()
    {
        var codec = new LengthPrefixCodec(new TcpHeaderOptions(TcpHeaderType.Binary4Byte, MaxPayloadLength: 100));
        var buffer = new ReadOnlySequence<byte>([0, 0, 0, 101, 1]);

        Assert.Throws<InvalidDataException>(() => codec.TryDecode(ref buffer, out _));
    }

    [Fact]
    public void Waits_for_complete_frame()
    {
        byte[] frame = LengthPrefixCodec.Default.Encode("hello"u8);
        var partial = new ReadOnlySequence<byte>(frame, 0, frame.Length - 1);

        Assert.False(LengthPrefixCodec.Default.TryDecode(ref partial, out _));
        Assert.Equal(frame.Length - 1, partial.Length);
    }
}

public class TcpFrameClientServerTests
{
    [Fact]
    public async Task Client_and_server_exchange_frames_both_ways()
    {
        await using var server = new TcpFrameServer(new IPEndPoint(IPAddress.Loopback, 0), LengthPrefixCodec.Default, new TcpFrameServerOptions(), NullLogger.Instance)
        {
            FrameReceived = (connection, payload) => connection.SendAsync(payload.Reverse().ToArray()),
        };
        await server.StartAsync();

        var received = new TaskCompletionSource<byte[]>();
        await using var client = new TcpFrameClient("127.0.0.1", server.LocalEndPoint.Port, LengthPrefixCodec.Default,
            new TcpFrameClientOptions { ReconnectDelay = TimeSpan.FromMilliseconds(50) }, NullLogger.Instance)
        {
            FrameReceived = payload => { received.TrySetResult(payload); return Task.CompletedTask; },
        };
        await client.StartAsync();
        await Wait.UntilAsync(() => client.IsConnected, "client terkoneksi");

        await client.SendAsync("abc"u8.ToArray());

        Assert.Equal("cba"u8.ToArray(), await Wait.ForAsync(received.Task));
    }

    [Fact]
    public async Task Client_reconnects_after_server_restart()
    {
        var server = new TcpFrameServer(new IPEndPoint(IPAddress.Loopback, 0), LengthPrefixCodec.Default, new TcpFrameServerOptions(), NullLogger.Instance);
        await server.StartAsync();
        int port = server.LocalEndPoint.Port;

        int connects = 0, disconnects = 0;
        await using var client = new TcpFrameClient("127.0.0.1", port, LengthPrefixCodec.Default,
            new TcpFrameClientOptions { ReconnectDelay = TimeSpan.FromMilliseconds(50) }, NullLogger.Instance)
        {
            Connected = _ => { Interlocked.Increment(ref connects); return Task.CompletedTask; },
            Disconnected = _ => { Interlocked.Increment(ref disconnects); return Task.CompletedTask; },
        };
        await client.StartAsync();
        await Wait.UntilAsync(() => client.IsConnected, "koneksi pertama");

        await server.StopAsync();
        await Wait.UntilAsync(() => !client.IsConnected && disconnects == 1, "terputus");

        await using var restarted = new TcpFrameServer(new IPEndPoint(IPAddress.Loopback, port), LengthPrefixCodec.Default, new TcpFrameServerOptions(), NullLogger.Instance);
        await restarted.StartAsync();
        await Wait.UntilAsync(() => client.IsConnected && connects == 2, "koneksi ulang");
    }

    [Fact]
    public async Task Send_without_connection_throws_instead_of_silently_failing()
    {
        await using var client = new TcpFrameClient("127.0.0.1", 1, LengthPrefixCodec.Default, new TcpFrameClientOptions(), NullLogger.Instance);

        await Assert.ThrowsAsync<NotConnectedException>(() => client.SendAsync("x"u8.ToArray()));
    }

    [Fact]
    public async Task Server_enforces_max_connections()
    {
        await using var server = new TcpFrameServer(new IPEndPoint(IPAddress.Loopback, 0), LengthPrefixCodec.Default,
            new TcpFrameServerOptions { MaxConnections = 1 }, NullLogger.Instance);
        await server.StartAsync();

        await using var first = new TcpFrameClient("127.0.0.1", server.LocalEndPoint.Port, LengthPrefixCodec.Default,
            new TcpFrameClientOptions { AutoReconnect = false }, NullLogger.Instance);
        await first.StartAsync();
        await Wait.UntilAsync(() => server.Connections.Count == 1, "koneksi pertama diterima");

        using var second = new System.Net.Sockets.TcpClient();
        await second.ConnectAsync(IPAddress.Loopback, server.LocalEndPoint.Port);
        byte[] buffer = new byte[1];
        int read = await second.GetStream().ReadAsync(buffer).AsTask().WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(0, read);
        Assert.Single(server.Connections);
    }
}
