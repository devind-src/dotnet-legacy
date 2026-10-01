using System.Globalization;
using System.Net;
using System.Net.Sockets;
using SyncNet.Template.Iso;
using SyncNetPro.Contracts;
using SyncNetPro.Iso8583;
using SyncNetPro.Sdk.Nodes;
using SyncNetPro.Sdk.Testing;

namespace SyncNet.Template.Tests;

/// <summary>
/// End-to-end tanpa Core: test bertindak sebagai pengirim ISO 8583 (<see cref="SimTcpClient"/>), SimCore (in-process)
/// bertindak sebagai Core dan membalas sesuai <c>Responder</c> di <c>simcore/simcore.json</c>.
/// </summary>
public sealed class InterfaceFlowTests : IAsyncLifetime
{
    private SimCore? _core;
    private IAsyncDisposable? _app;
    private int _port;

    public async ValueTask InitializeAsync()
    {
        SimCoreOptions options = SimCoreOptions.Load(Path.Combine(AppContext.BaseDirectory, "simcore", "simcore.json"));
        options.LogServicesPort = 0;
        options.Interface = null;
        foreach (SimNodeOptions node in options.Nodes) node.PortIn = node.PortOut = 0;

        _core = await SimCore.StartAsync(options, cancellationToken: TestContext.Current.CancellationToken);
        _port = FreePort();
        RemoteConnectionInfo listener = new()
        {
            Name = "TEMPLATE_NODE_TCP",
            NodeName = "TEMPLATE_NODE",
            Protocol = ConnectionProtocol.Tcp2ByteExcludeHeader,
            Role = ConnectionRole.Server,
            Host = "127.0.0.1",
            Port = _port,
            AlwaysConnected = true,
        };
        _app = await SimInterfaceHost.StartAsync<AcquirerInterface>(_core, [listener], services: s => s.AddAcquirerServices());
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null) await _app.DisposeAsync();
        if (_core is not null) await _core.DisposeAsync();
    }

    [Fact]
    public async Task Inquiry_is_forwarded_to_core_and_answered()
    {
        IsoMessage reply = await SendAsync(Request("0200", "380000", 0));

        Assert.Equal("0210", reply.Mti);
        Assert.Equal("00", reply[39]);
        Assert.Equal("BUDI SANTOSO R1 900VA", reply[48]);

        CoreRequest atCore = Assert.Single(_core!.Node("TEMPLATE_NODE").Received);
        Assert.Equal(TranType.Inquiry, atCore.TranType);
        Assert.Equal("TERM0001", atCore.TerminalId);
        Assert.Equal("532110000001", atCore.ToAccountNumber);
        Assert.Empty(_core.ContractWarnings);
    }

    [Fact]
    public async Task Core_response_code_is_returned_to_sender()
    {
        Assert.Equal("61", (await SendAsync(Request("0200", "500000", 20_000_000)))[39]);

        IsoMessage approved = await SendAsync(Request("0200", "500000", 125_000));
        Assert.Equal("00", approved[39]);
        Assert.Equal("A12345", approved[38]);
    }

    [Fact]
    public async Task Network_management_is_answered_without_core()
    {
        IsoMessage echo = new IsoMessage(AcquirerIsoSpec.Instance, "0800").Set(7, "0930101530").Set(11, "000001").Set(70, "301");

        IsoMessage reply = await SendAsync(echo);

        Assert.Equal("0810", reply.Mti);
        Assert.Equal("00", reply[39]);
        Assert.Empty(_core!.Node("TEMPLATE_NODE").Received);
    }

    private static IsoMessage Request(string mti, string processingCode, long amount) => new IsoMessage(AcquirerIsoSpec.Instance, mti)
        .Set(3, processingCode)
        .Set(4, amount.ToString("D12", CultureInfo.InvariantCulture))
        .Set(7, "0930101530")
        .Set(11, Random.Shared.Next(1, 999_999).ToString("D6", CultureInfo.InvariantCulture))
        .Set(32, "008")
        .Set(37, "260930000123")
        .Set(41, "TERM0001")
        .Set(42, "MERCHANT01     ")
        .Set(48, "532110000001")
        .Set(49, "360");

    private async Task<IsoMessage> SendAsync(IsoMessage request)
    {
        byte[] reply = await SimTcpClient.SendAsync("127.0.0.1", _port, request.Pack(), timeout: TimeSpan.FromSeconds(10),
            cancellationToken: TestContext.Current.CancellationToken);
        return IsoMessage.Parse(AcquirerIsoSpec.Instance, reply);
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
