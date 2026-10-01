using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using SyncNetPro.Iso8583;
using SyncNetPro.Sdk.Testing;

namespace SyncNetPro.SimCore.Tests;

public class StubTests
{
    private static readonly IsoStubSpec Spec = new()
    {
        Fields =
        [
            new IsoStubField { Number = 11, LengthType = IsoLengthType.Fixed, Content = IsoFieldContent.N, Length = 6 },
            new IsoStubField { Number = 39, LengthType = IsoLengthType.Fixed, Content = IsoFieldContent.An, Length = 2 },
            new IsoStubField { Number = 41, LengthType = IsoLengthType.Fixed, Content = IsoFieldContent.Ans, Length = 8 },
        ],
    };

    [Fact]
    public async Task Iso_stub_answers_with_response_mti_correlation_fields_and_overrides()
    {
        var options = new RemoteStubOptions
        {
            Name = "BILLER",
            Iso = Spec,
            Replies =
            [
                new StubReply { Mti = "0800", IsoSet = new() { [39] = "00" } },
                new StubReply { Mti = "0200", IsoMatch = new() { [3] = "38" }, IsoSet = new() { [39] = "00", [48] = "NAMA PELANGGAN {{field:11}}" } },
                new StubReply { Mti = "0200", IsoSet = new() { [39] = "05" } },
            ],
        };
        await using RemoteStub stub = await RemoteStub.StartAsync(options, IPAddress.Loopback, NullLogger.Instance, TestContext.Current.CancellationToken);
        IsoSpec spec = Spec.Build();

        IsoMessage inquiry = await SendAsync(stub, new IsoMessage(spec, "0200").Set(3, "380000").Set(11, "000123").Set(41, "TERM0001"));
        IsoMessage payment = await SendAsync(stub, new IsoMessage(spec, "0200").Set(3, "500000").Set(11, "000124").Set(41, "TERM0001"));
        IsoMessage echo = await SendAsync(stub, new IsoMessage(spec, "0800").Set(11, "000125").Set(70, "301"));

        Assert.Equal(("0210", "000123", "TERM0001", "00", "NAMA PELANGGAN 000123"), (inquiry.Mti, inquiry[11], inquiry[41], inquiry[39], inquiry[48]));
        Assert.Equal(("0210", "05"), (payment.Mti, payment[39]));
        Assert.Equal(("0810", "00", "301"), (echo.Mti, echo[39], echo[70]));
        Assert.Equal(3, stub.Received.Count);

        async Task<IsoMessage> SendAsync(RemoteStub s, IsoMessage request) =>
            IsoMessage.Parse(spec, await SimTcpClient.SendAsync("127.0.0.1", s.Port, request.Pack(), cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Http_stub_reply_can_echo_json_request_fields()
    {
        var options = new RemoteStubOptions
        {
            Name = "BILLER_HTTP",
            Type = RemoteStubType.Http,
            Replies = [new StubReply { Path = "/bill/inquiry", Reply = """{"rc":"00","ref":"{{json:trace_number}}","missing":"{{json:nope}}"}""" }],
        };
        await using RemoteStub stub = await RemoteStub.StartAsync(options, IPAddress.Loopback, NullLogger.Instance, TestContext.Current.CancellationToken);
        using var http = new HttpClient();

        using HttpResponseMessage response = await http.PostAsync(new Uri($"http://127.0.0.1:{stub.Port}/bill/inquiry"),
            new StringContent("""{"trace_number":"000777"}""", Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);
        JObject body = JObject.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        Assert.Equal("000777", (string?)body["ref"]);
        Assert.Equal(string.Empty, (string?)body["missing"]);
    }

    [Fact]
    public async Task Tcp_client_times_out_when_nobody_answers()
    {
        var options = new RemoteStubOptions { Name = "SILENT", Replies = [new StubReply { Silent = true }] };
        await using RemoteStub stub = await RemoteStub.StartAsync(options, IPAddress.Loopback, NullLogger.Instance, TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<TimeoutException>(() => SimTcpClient.SendAsync("127.0.0.1", stub.Port, "PING"u8.ToArray(), timeout: TimeSpan.FromMilliseconds(300),
            cancellationToken: TestContext.Current.CancellationToken));
    }
}
