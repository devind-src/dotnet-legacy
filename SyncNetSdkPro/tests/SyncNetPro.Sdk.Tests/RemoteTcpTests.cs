using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using SyncNetPro.Contracts;
using SyncNetPro.Sdk.Nodes;
using SyncNetPro.Sdk.Remote;
using SyncNetPro.Sdk.Tests.Infrastructure;
using SyncNetPro.Sdk.Transport;

namespace SyncNetPro.Sdk.Tests;

public class RemoteCodecTests
{
    public static TheoryData<string, int, string, string, bool, string, string, string> LegacySetProtocol()
    {
        var data = new TheoryData<string, int, string, string, bool, string, string, string>();
        foreach (JToken c in JArray.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Golden", "setprotocol.json"))))
        {
            data.Add((string)c["source"]!, (int)c["length"]!, (string)c["mode"]!, (string)c["format"]!, (bool)c["hiLo"]!,
                (string)c["headerType"]!, (string)c["lengthMode"]!, (string)c["endian"]!);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(LegacySetProtocol))]
    public void Connection_mapping_matches_legacy_SetProtocol(string source, int length, string mode, string format, bool hiLo,
        string headerType, string lengthMode, string endian)
    {
        _ = source;
        ConnectionProtocol protocol = (length, mode) switch
        {
            (2, "ExcludeHeader") => ConnectionProtocol.Tcp2ByteExcludeHeader,
            (2, _) => ConnectionProtocol.Tcp2ByteIncludeHeader,
            (4, "ExcludeHeader") => ConnectionProtocol.Tcp4ByteExcludeHeader,
            _ => ConnectionProtocol.Tcp4ByteIncludeHeader,
        };
        var info = new RemoteConnectionInfo
        {
            Name = "C",
            Protocol = protocol,
            HeaderFormat = format == "BCD" ? TcpHeaderFormat.Bcd : TcpHeaderFormat.Ascii,
            HeaderHighLow = hiLo,
        };

        var codec = Assert.IsType<LengthPrefixCodec>(RemoteCodecs.ForConnection(info));

        Assert.Equal(headerType, codec.Options.HeaderType.ToString());
        Assert.Equal(lengthMode, codec.Options.LengthMode.ToString());
        Assert.Equal(endian, codec.Options.Endian.ToString());
    }

    [Fact]
    public void Legacy_golden_covers_client_and_listener() => Assert.Equal(32, LegacySetProtocol().Count);

    [Fact]
    public void No_header_protocol_really_has_no_header()
    {
        // Perbaikan B2: SDK lama tetap memakai header 2 byte untuk "tanpa header".
        ITcpFrameCodec codec = RemoteCodecs.ForConnection(new RemoteConnectionInfo { Protocol = ConnectionProtocol.TcpHeaderNone });

        Assert.Same(NoHeaderCodec.Instance, codec);
        Assert.Equal("abc"u8.ToArray(), codec.Encode("abc"u8));
    }

    [Fact]
    public void Custom_protocol_defaults_to_legacy_two_byte_header()
    {
        var codec = Assert.IsType<LengthPrefixCodec>(RemoteCodecs.ForConnection(new RemoteConnectionInfo { Protocol = ConnectionProtocol.TcpHeaderCustom }));
        Assert.Equal(TcpHeaderType.Binary2Byte, codec.Options.HeaderType);
    }

    [Fact]
    public void Web_service_is_not_tcp() =>
        Assert.Throws<NotSupportedException>(() => RemoteCodecs.ForConnection(new RemoteConnectionInfo { Protocol = ConnectionProtocol.WebService }));
}

/// <summary>Koneksi TCP ke sistem eksternal end-to-end (FakeCore ↔ interface ↔ biller/EDC tiruan).</summary>
public class RemoteTcpTests
{
    private static readonly LengthPrefixCodec BcdCodec = new(new TcpHeaderOptions(TcpHeaderType.Bcd2Byte));

    private static RemoteConnectionInfo Client(string name, string node, int port, bool persistent = true,
        ConnectionProtocol protocol = ConnectionProtocol.Tcp2ByteExcludeHeader, TcpHeaderFormat format = TcpHeaderFormat.Bcd) => new()
    {
        Name = name,
        NodeName = node,
        Protocol = protocol,
        Role = ConnectionRole.Client,
        HeaderFormat = format,
        Host = "127.0.0.1",
        Port = port,
        AlwaysConnected = persistent,
        RetryDelaySeconds = 0,
    };

    private static async Task<TcpFrameServer> BillerAsync(ITcpFrameCodec codec, Func<FramedConnection, byte[], Task> onFrame)
    {
        var server = new TcpFrameServer(new IPEndPoint(IPAddress.Loopback, 0), codec, new TcpFrameServerOptions(), NullLogger.Instance) { FrameReceived = onFrame };
        await server.StartAsync();
        return server;
    }

    private static byte[] Text(string value) => Encoding.UTF8.GetBytes(value);

    [Fact]
    public async Task Outbound_request_is_forwarded_and_correlated_reply_is_returned()
    {
        await using FakeCore core = await FakeCore.StartAsync();
        await using TcpFrameServer biller = await BillerAsync(BcdCodec, async (c, frame) =>
        {
            string text = Encoding.UTF8.GetString(frame);
            if (!text.StartsWith("REQ", StringComparison.Ordinal)) return;
            string key = text.Split('|')[1];
            await c.SendAsync(Text("0800|PING"));          // pesan spontan (bukan balasan)
            await c.SendAsync(Text($"{key}|APPROVED"));    // balasan terkorelasi
        });

        var source = new MutableNodeSource
        {
            Nodes = [Messages.Node("BILLER", NodeCategory.BillerIssuer, core)],
            Connections = [Client("BILLER_C1", "BILLER", biller.LocalEndPoint.Port)],
        };
        await using var app = await TestHost<ScriptedInterface>.StartAsync([], services: s => s.AddSingleton<INodeConfigurationSource>(source));
        app.Handler.UsePipeCorrelation = true;
        app.Handler.OnRequest = async ctx =>
        {
            string key = "K" + ctx.Request.TraceNumber;
            byte[] reply = await ctx.Remote.SendAndReceiveAsync(Text($"REQ|{key}"), key);
            return ctx.Request.ToResponse("00", Encoding.UTF8.GetString(reply));
        };
        await Wait.UntilAsync(() => core.SinkConnected && app.Handler.Events.Contains("connected:BILLER_C1"), "terkoneksi ke Core dan biller");

        await core.SendToInterfaceAsync(Messages.Request());
        CoreResponse response = await core.ReceiveResponseAsync();

        Assert.Equal("00", response.ResponseCode);
        Assert.Equal("K000123|APPROVED", response.ResponseMessage);
        await Wait.UntilAsync(() => app.Handler.Events.Contains("message:0800|PING"), "pesan spontan ke OnRemoteMessageAsync");
    }

    [Fact]
    public async Task Send_and_receive_times_out()
    {
        await using FakeCore core = await FakeCore.StartAsync();
        await using TcpFrameServer biller = await BillerAsync(BcdCodec, (_, _) => Task.CompletedTask);
        var source = new MutableNodeSource
        {
            Nodes = [Messages.Node("BILLER", NodeCategory.BillerIssuer, core)],
            Connections = [Client("BILLER_C1", "BILLER", biller.LocalEndPoint.Port)],
        };
        await using var app = await TestHost<ScriptedInterface>.StartAsync([], services: s => s.AddSingleton<INodeConfigurationSource>(source));
        app.Handler.UsePipeCorrelation = true;
        var registry = app.Services.GetRequiredService<IRemoteRegistry>();
        await Wait.UntilAsync(() => registry.GetNode("BILLER").IsConnected, "terkoneksi ke biller");

        await Assert.ThrowsAsync<TimeoutException>(() =>
            registry.GetNode("BILLER").SendAndReceiveAsync(Text("REQ|X"), "X", TimeSpan.FromMilliseconds(200)));
    }

    [Fact]
    public async Task Auto_sign_on_runs_after_connect_and_status_is_reported()
    {
        await using FakeCore core = await FakeCore.StartAsync();
        var received = new ConcurrentQueue<string>();
        await using TcpFrameServer biller = await BillerAsync(BcdCodec, (_, frame) => { received.Enqueue(Encoding.UTF8.GetString(frame)); return Task.CompletedTask; });
        var status = new RecordingStatusReporter();
        var source = new MutableNodeSource
        {
            Nodes = [Messages.Node("BILLER", NodeCategory.BillerIssuer, core) with { AutoSignOn = true }],
            Connections = [Client("BILLER_C1", "BILLER", biller.LocalEndPoint.Port)],
        };
        var app = await TestHost<ScriptedInterface>.StartAsync([],
            o => o.Remote.AutoSignOnDelay = TimeSpan.FromMilliseconds(50),
            s => s.AddSingleton<INodeConfigurationSource>(source).AddSingleton<INodeStatusReporter>(status));
        app.Handler.OnSignOn = ctx => ((IRemoteTcpConnection)ctx.Connection).SendAsync(Text("SIGNON"));

        await Wait.UntilAsync(() => received.Contains("SIGNON"), "sign-on terkirim ke biller");
        Assert.True(status.Has("remote:BILLER:True"));
        await Wait.UntilAsync(() => status.Has("conn:BILLER_C1:True"), "status koneksi UP");

        await app.DisposeAsync();
        Assert.True(status.Has("remote:BILLER:False"));
        Assert.True(status.Has("conn:BILLER_C1:False"));
    }

    [Fact]
    public async Task Non_persistent_client_uses_configured_header_and_new_socket_per_message()
    {
        // Perbaikan B1: NodeClient lama selalu memakai header 2 byte biner.
        var codec = new LengthPrefixCodec(new TcpHeaderOptions(TcpHeaderType.Binary4Byte, TcpLengthMode.Include));
        int sockets = 0;
        await using FakeCore core = await FakeCore.StartAsync();
        await using TcpFrameServer biller = await BillerAsync(codec, (c, frame) => c.SendAsync(Text("RSP:" + Encoding.UTF8.GetString(frame))));
        biller.Connected = _ => { Interlocked.Increment(ref sockets); return Task.CompletedTask; };
        var source = new MutableNodeSource
        {
            Nodes = [Messages.Node("BILLER", NodeCategory.BillerIssuer, core)],
            Connections = [Client("BILLER_NP", "BILLER", biller.LocalEndPoint.Port, persistent: false, ConnectionProtocol.Tcp4ByteIncludeHeader)],
        };
        await using var app = await TestHost<ScriptedInterface>.StartAsync([], services: s => s.AddSingleton<INodeConfigurationSource>(source));
        IRemoteNode remote = app.Services.GetRequiredService<IRemoteRegistry>().GetNode("BILLER");

        byte[] first = await remote.SendAndReceiveAsync(Text("A"), "unused");
        byte[] second = await remote.SendAndReceiveAsync(Text("B"), "unused");

        Assert.Equal("RSP:A", Encoding.UTF8.GetString(first));
        Assert.Equal("RSP:B", Encoding.UTF8.GetString(second));
        Assert.Equal(2, sockets);
    }

    [Fact]
    public async Task Non_persistent_send_delivers_reply_to_handler()
    {
        await using FakeCore core = await FakeCore.StartAsync();
        await using TcpFrameServer biller = await BillerAsync(BcdCodec, (c, _) => c.SendAsync(Text("0810|ECHO")));
        var source = new MutableNodeSource
        {
            Nodes = [Messages.Node("BILLER", NodeCategory.BillerIssuer, core)],
            Connections = [Client("BILLER_NP", "BILLER", biller.LocalEndPoint.Port, persistent: false)],
        };
        await using var app = await TestHost<ScriptedInterface>.StartAsync([], services: s => s.AddSingleton<INodeConfigurationSource>(source));

        await app.Services.GetRequiredService<IRemoteRegistry>().GetNode("BILLER").SendAsync(Text("0800|ECHO"));

        await Wait.UntilAsync(() => app.Handler.Events.Contains("message:0810|ECHO"), "balasan ke OnRemoteMessageAsync");
    }

    [Fact]
    public async Task Tcp_server_receives_request_forwards_to_core_and_replies()
    {
        await using FakeCore core = await FakeCore.StartAsync();
        var source = new MutableNodeSource
        {
            Nodes = [Messages.Node("EDC", NodeCategory.Merchant, core)],
            Connections =
            [
                new RemoteConnectionInfo
                {
                    Name = "EDC_SRV", NodeName = "EDC", Protocol = ConnectionProtocol.Tcp2ByteExcludeHeader, Role = ConnectionRole.Server,
                    HeaderFormat = TcpHeaderFormat.Ascii, Host = "127.0.0.1", Port = 0, AlwaysConnected = true,
                },
            ],
        };
        await using var app = await TestHost<ScriptedInterface>.StartAsync([], services: s => s.AddSingleton<INodeConfigurationSource>(source));
        app.Handler.OnRemote = async ctx =>
        {
            CoreResponse rsp = await ctx.SendToCoreAsync(Messages.Request(Encoding.UTF8.GetString(ctx.Message)));
            await ctx.ReplyAsync(Text($"RC={rsp.ResponseCode}"));
        };
        var server = (TcpServerConnection)app.Services.GetRequiredService<IRemoteRegistry>().GetNode("EDC").GetConnection("EDC_SRV");
        await Wait.UntilAsync(() => core.SourceConnected, "kanal inbound terkoneksi");

        await using var edc = new TcpFrameClient("127.0.0.1", server.LocalEndPoint.Port, LengthPrefixCodec.Default, new TcpFrameClientOptions(), NullLogger.Instance);
        var reply = new TaskCompletionSource<string>();
        edc.FrameReceived = frame => { reply.TrySetResult(Encoding.UTF8.GetString(frame)); return Task.CompletedTask; };
        await edc.StartAsync();
        await Wait.UntilAsync(() => edc.IsConnected, "EDC terkoneksi");
        await edc.SendAsync(Text("000555"));

        Assert.Equal("RC=00", await Wait.ForAsync(reply.Task));
        Assert.True(core.SourceRequests.TryDequeue(out CoreRequest? sent));
        Assert.Equal("000555", sent.TraceNumber);
        Assert.Equal("EDC_SRV", sent.PrivateData!.ConnectionName);
        Assert.Equal("127.0.0.1", sent.PrivateData.IpExternal);
    }

    [Fact]
    public async Task Resync_reopens_changed_connection_and_closes_removed_one()
    {
        await using FakeCore core = await FakeCore.StartAsync();
        await using TcpFrameServer first = await BillerAsync(BcdCodec, (_, _) => Task.CompletedTask);
        await using TcpFrameServer second = await BillerAsync(BcdCodec, (_, _) => Task.CompletedTask);
        var source = new MutableNodeSource
        {
            Nodes = [Messages.Node("BILLER", NodeCategory.BillerIssuer, core)],
            Connections = [Client("C1", "BILLER", first.LocalEndPoint.Port)],
        };
        await using var app = await TestHost<ScriptedInterface>.StartAsync([], services: s => s.AddSingleton<INodeConfigurationSource>(source));
        await Wait.UntilAsync(() => first.HasConnections, "terkoneksi ke server pertama");

        source.Connections = [Client("C1", "BILLER", second.LocalEndPoint.Port)];
        await app.Runtime.ReloadAsync();
        await Wait.UntilAsync(() => second.HasConnections && !first.HasConnections, "pindah ke server kedua");

        source.Connections = [];
        await app.Runtime.ReloadAsync();
        await Wait.UntilAsync(() => !second.HasConnections, "koneksi ditutup");
        Assert.Throws<RemoteUnavailableException>(() => app.Services.GetRequiredService<IRemoteRegistry>().GetNode("BILLER").Tcp);
    }

    [Fact]
    public async Task Server_role_send_without_client_is_unavailable()
    {
        await using FakeCore core = await FakeCore.StartAsync();
        var source = new MutableNodeSource
        {
            Nodes = [Messages.Node("EDC", NodeCategory.Merchant, core)],
            Connections = [new RemoteConnectionInfo { Name = "S", NodeName = "EDC", Role = ConnectionRole.Server, Host = "127.0.0.1", Port = 0 }],
        };
        await using var app = await TestHost<ScriptedInterface>.StartAsync([], services: s => s.AddSingleton<INodeConfigurationSource>(source));

        await Assert.ThrowsAsync<RemoteUnavailableException>(() => app.Services.GetRequiredService<IRemoteRegistry>().GetNode("EDC").SendAsync(Text("x")));
    }
}
