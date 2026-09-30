using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using SyncNetPro.Contracts;
using SyncNetPro.Sdk.Core;
using SyncNetPro.Sdk.Nodes;
using SyncNetPro.Sdk.Testing;
using SyncNetPro.Sdk.Transport;
using CoreSim = SyncNetPro.Sdk.Testing.SimCore;

namespace SyncNetPro.SimCore.Tests;

public class SimCoreSinkTests
{
    [Fact]
    public async Task Interface_response_is_correlated_and_valid()
    {
        await using CoreSim core = await CoreSim.StartAsync(Samples.Options());
        await using var app = await SimInterfaceHost.StartAsync<BillerInterface>(core);

        SimResult result = await core.SendAsync("BILLER", Samples.Request());

        Assert.Equal(SimOutcome.Responded, result.Outcome);
        Assert.Equal("00", result.Response!.ResponseCode);
        Assert.Equal("0210", result.Response.MessageType);
        Assert.Empty(result.Warnings);
        Assert.Empty(core.ContractWarnings);
    }

    [Fact]
    public async Task Link_down_when_interface_is_not_connected()
    {
        await using CoreSim core = await CoreSim.StartAsync(Samples.Options());

        SimResult result = await core.SendAsync("BILLER", Samples.Request());

        Assert.Equal(SimOutcome.LinkDown, result.Outcome);
        Assert.Equal("91", result.Response!.ResponseCode);
        Assert.Equal(AuthorizedBy.Internal, result.Response.AuthorizedBy);
    }

    [Fact]
    public async Task Duplicate_key_while_pending_is_answered_94()
    {
        await using CoreSim core = await CoreSim.StartAsync(Samples.Options());
        await using var app = await SimInterfaceHost.StartAsync<BillerInterface>(core);
        app.Handler.Delay = TimeSpan.FromMilliseconds(500);

        Task<SimResult> first = core.SendAsync("BILLER", Samples.Request());
        await Task.Delay(100);
        SimResult second = await core.SendAsync("BILLER", Samples.Request());

        Assert.Equal(SimOutcome.Duplicate, second.Outcome);
        Assert.Equal("94", second.Response!.ResponseCode);
        Assert.Equal(SimOutcome.Responded, (await first).Outcome);
        Assert.Equal(1, app.Handler.Requests);
    }

    [Fact]
    public async Task Timeout_marks_late_response_and_sends_auto_reversal()
    {
        SimCoreOptions options = Samples.Options(timeoutSeconds: 1);
        options.Nodes[0].AutoReversal = true;
        await using CoreSim core = await CoreSim.StartAsync(options);
        await using var app = await SimInterfaceHost.StartAsync<BillerInterface>(core);
        app.Handler.Delay = TimeSpan.FromMilliseconds(1500);

        SimResult result = await core.SendAsync("BILLER", Samples.Request(TranType.Inquiry));

        Assert.Equal(SimOutcome.Timeout, result.Outcome);
        await WaitAsync(() => core.Messages.Any(m => m.Note?.StartsWith("tidak dikenal/terlambat", StringComparison.Ordinal) == true), "respons terlambat tercatat");
        await WaitAsync(() => core.Messages.Any(m => m.Note == "auto-reversal (auto_reversal node aktif)"), "auto-reversal dikirim");
        SimMessage reversal = core.Messages.First(m => m.Note == "auto-reversal (auto_reversal node aktif)");
        JObject json = JObject.Parse(reversal.Json);
        Assert.Equal("REV", (string?)json["tran_type"]);
        Assert.Equal("0400", (string?)json["msgtype"]);
        Assert.Equal("INQ0930101530000123", (string?)json["original_data"]);
    }

    [Fact]
    public async Task Traces_from_interface_reach_fake_log_services()
    {
        await using CoreSim core = await CoreSim.StartAsync(Samples.Options());
        await using var app = await SimInterfaceHost.StartAsync<BillerInterface>(core);

        await core.SendAsync("BILLER", Samples.Request());

        await WaitAsync(() => core.Traces.Any(t => t.Json.Contains("<INQ> Message to BILLER stub", StringComparison.Ordinal)), "trace diterima");
    }

    [Fact]
    public async Task Command_port_of_interface_is_reachable()
    {
        await using CoreSim core = await CoreSim.StartAsync(Samples.Options());
        await using var app = await SimInterfaceHost.StartAsync<BillerInterface>(core, configure: o => o.Version = "v7");

        Assert.Equal("v7", await core.CommandAsync("VERSION"));
    }

    [Fact]
    public async Task Tcp_remote_stub_answers_interface()
    {
        SimCoreOptions options = Samples.Options();
        options.RemoteStubs = [new RemoteStubOptions { Name = "BILLER_STUB", Replies = [new StubReply { Contains = "PAY|", Reply = "PAY|000123|00" }] }];
        await using CoreSim core = await CoreSim.StartAsync(options);
        RemoteConnectionInfo connection = new()
        {
            Name = "BILLER_C1", NodeName = "BILLER", Role = ConnectionRole.Client, Host = "127.0.0.1",
            Port = core.RemoteStubs[0].Port, AlwaysConnected = true,
        };
        await using var app = await SimInterfaceHost.StartAsync<BillerInterface>(core, [connection]);
        await WaitAsync(() => app.Services.GetRequiredService<SyncNetPro.Sdk.Remote.IRemoteRegistry>().GetNode("BILLER").IsConnected, "terkoneksi ke stub");

        SimResult result = await core.SendAsync("BILLER", Samples.Request(TranType.Payment));

        Assert.True(result.Outcome == SimOutcome.Responded,
            $"outcome={result.Outcome} stubRequests={core.RemoteStubs[0].RequestCount} stubPort={core.RemoteStubs[0].Port} messages:\n" +
            string.Join("\n", core.Messages.Select(m => $"{m.Channel} {m.Direction} {m.Note}")));
        Assert.Equal("00", result.Response!.ResponseCode);
        Assert.Equal(1, core.RemoteStubs[0].RequestCount);
    }

    internal static async Task WaitAsync(Func<bool> condition, string because, int timeoutMs = 5000)
    {
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline) throw new TimeoutException(because);
            await Task.Delay(20);
        }
    }
}

public class SimCoreSourceTests
{
    private static async Task<(CoreSim Core, SimInterfaceHost<ChannelInterface> App, ICoreClient Client)> StartAsync(SourceResponderOptions responder)
    {
        SimCoreOptions options = Samples.Options(NodeCategory.Merchant);
        options.Nodes[0].Responder = responder;
        CoreSim core = await CoreSim.StartAsync(options);
        var app = await SimInterfaceHost.StartAsync<ChannelInterface>(core);
        return (core, app, app.Services.GetRequiredService<ICoreClient>());
    }

    [Fact]
    public async Task Fixed_responder_approves()
    {
        var (core, app, client) = await StartAsync(new SourceResponderOptions());
        await using (core)
        await using (app)
        {
            CoreResponse response = await client.SendAsync("BILLER", Samples.Request());

            Assert.Equal("00", response.ResponseCode);
            Assert.Single(core.Node("BILLER").Received);
        }
    }

    [Fact]
    public async Task Rules_select_response_by_tran_type_and_amount()
    {
        var responder = new SourceResponderOptions
        {
            Mode = SourceResponseMode.Rules,
            ResponseCode = "05",
            Rules =
            [
                new ResponseRule { TranType = "INQ", ResponseCode = "00", AdditionalData = new() { ["customer_name"] = "SITI" } },
                new ResponseRule { TranType = "PAY", MinAmount = 1_000_000, ResponseCode = "61", ResponseMessage = "Exceeds limit" },
            ],
        };
        var (core, app, client) = await StartAsync(responder);
        await using (core)
        await using (app)
        {
            CoreResponse inquiry = await client.SendAsync("BILLER", Samples.Request(TranType.Inquiry, "000001"));
            CoreRequest big = Samples.Request(TranType.Payment, "000002");
            big.Amount = 2_000_000m;
            CoreResponse bigPayment = await client.SendAsync("BILLER", big);
            CoreRequest small = Samples.Request(TranType.Payment, "000003");
            small.Amount = 10m;
            CoreResponse smallPayment = await client.SendAsync("BILLER", small);

            Assert.Equal("00", inquiry.ResponseCode);
            Assert.True(inquiry.TryGetAdditionalData("customer_name", out string? name));
            Assert.Equal("SITI", name);
            Assert.Equal("61", bigPayment.ResponseCode);
            Assert.Equal("Exceeds limit", bigPayment.ResponseMessage);
            Assert.Equal("05", smallPayment.ResponseCode);
        }
    }

    [Fact]
    public async Task None_responder_lets_interface_time_out()
    {
        var (core, app, client) = await StartAsync(new SourceResponderOptions { Mode = SourceResponseMode.None });
        await using (core)
        await using (app)
        {
            await Assert.ThrowsAsync<TimeoutException>(() =>
                client.SendAsync("BILLER", Samples.Request(), new CoreSendOptions { Timeout = TimeSpan.FromMilliseconds(300) }));
        }
    }
}

public class ContractValidatorTests
{
    [Fact]
    public void Valid_response_has_no_warnings()
    {
        string json = CoreMessageCodec.Default.Serializer.Serialize(Samples.Request().ToResponse("00"));
        Assert.Empty(ContractValidator.ValidateResponse(JObject.Parse(json)));
    }

    [Fact]
    public void Flags_extra_properties_wrong_types_and_missing_fields()
    {
        JObject json = JObject.Parse(CoreMessageCodec.Default.Serializer.Serialize(Samples.Request().ToResponse("00")));
        json["customer_name"] = "BUDI";
        json["amount_tran"] = "150000";
        json["resp_code"] = "";
        ((JObject)json["private_data"]!)["mode_timeout"] = "1";
        json["terminal_id"] = null;

        IReadOnlyList<string> warnings = ContractValidator.ValidateResponse(json);

        Assert.Contains(warnings, w => w.Contains("'customer_name'", StringComparison.Ordinal) && w.Contains("additional_data", StringComparison.Ordinal));
        Assert.Contains(warnings, w => w.Contains("'amount_tran' bertipe String", StringComparison.Ordinal));
        Assert.Contains(warnings, w => w.StartsWith("resp_code kosong", StringComparison.Ordinal));
        Assert.Contains(warnings, w => w.Contains("'private_data.mode_timeout'", StringComparison.Ordinal));
        Assert.Contains(warnings, w => w.StartsWith("terminal_id kosong", StringComparison.Ordinal));
    }

    [Fact]
    public void Additional_data_content_is_free_form()
    {
        JObject json = JObject.Parse(CoreMessageCodec.Default.Serializer.Serialize(Samples.Request().ToResponse("00").SetAdditionalData("x", new { a = 1 })));
        Assert.Empty(ContractValidator.ValidateResponse(json));
    }
}

public class SimCoreOptionsTests
{
    [Fact]
    public void Loads_json_with_enums_and_header_options()
    {
        string file = Path.GetTempFileName();
        File.WriteAllText(file, """
            {
              "AppName": "X",
              "Nodes": [ { "Name": "N", "Category": "Merchant", "PortIn": 1, "Responder": { "Mode": "Rules", "Rules": [ { "TranType": "INQ", "ResponseCode": "00" } ] } } ],
              "RemoteStubs": [ { "Name": "S", "Type": "Tcp", "Header": { "HeaderType": "Bcd2Byte", "LengthMode": "Include" } } ]
            }
            """);

        SimCoreOptions options = SimCoreOptions.Load(file);

        Assert.Equal(NodeCategory.Merchant, options.Nodes[0].Category);
        Assert.Equal(SourceResponseMode.Rules, options.Nodes[0].Responder.Mode);
        Assert.Equal(TcpHeaderType.Bcd2Byte, options.RemoteStubs[0].Header!.HeaderType);
        Assert.Equal(TcpLengthMode.Include, options.RemoteStubs[0].Header!.LengthMode);
        File.Delete(file);
    }

    [Fact]
    public void Rejects_typos_and_duplicate_nodes()
    {
        string file = Path.GetTempFileName();
        File.WriteAllText(file, """{ "Nodez": [] }""");
        Assert.Throws<InvalidDataException>(() => SimCoreOptions.Load(file));

        File.WriteAllText(file, """{ "Nodes": [ { "Name": "A" }, { "Name": "A" } ] }""");
        Assert.Throws<InvalidDataException>(() => SimCoreOptions.Load(file));
        File.Delete(file);
    }
}

