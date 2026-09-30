using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using SyncNetPro.Contracts;
using SyncNetPro.Sdk;
using SyncNetPro.Sdk.Core;
using SyncNetPro.Sdk.Nodes;
using SyncNetPro.Sdk.Testing;
using CoreSim = SyncNetPro.Sdk.Testing.SimCore;

namespace SyncNetPro.Hsm.Tests;

/// <summary>Interface contoh: translate PIN block via HSM sebelum membalas Core.</summary>
public sealed class PinInterface(IHsmClient hsm) : SyncNetInterface
{
    public override async Task<CoreResponse?> OnCoreRequestAsync(CoreRequestContext context, CancellationToken cancellationToken)
    {
        HsmPinResult pin = await hsm.TranslatePinBlockAsync("CORE", context.Node.Name, context.Request.Security?.PinData ?? string.Empty, context.Request.Pan, cancellationToken);
        CoreResponse response = context.Request.ToResponse(pin.IsSuccess ? "00" : pin.ResponseCode);
        response.Security ??= new Security();
        response.Security.PinData = pin.DestinationPinBlock;
        return response;
    }
}

public sealed class NoopInterface : SyncNetInterface;

public class HsmClientTests
{
    private static readonly JObject Golden = JObject.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Golden", "hsm-requests.json")));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static SimCoreOptions Options(SimHsmOptions? hsm = null) => new()
    {
        AppName = "hsm-test",
        Nodes = [new SimNodeOptions { Name = "BANK", Category = NodeCategory.BillerIssuer }],
        Hsm = hsm ?? new SimHsmOptions(),
    };

    [Fact]
    public void Request_json_is_identical_to_legacy_hsm_service()
    {
        Assert.Equal((string)Golden["generate-key"]!, HsmClient.Serialize(new HsmRequests.GenerateKey("BILLER_ABC", null)));
        Assert.Equal((string)Golden["generate-key-terminal"]!, HsmClient.Serialize(new HsmRequests.GenerateKey(null, "TERM0001")));
        Assert.Equal((string)Golden["translate-key"]!, HsmClient.Serialize(new HsmRequests.UpdateKey("BILLER_ABC", "U0123456789ABCDEF0123456789ABCDEF")));
        Assert.Equal((string)Golden["translate-pinblock"]!, HsmClient.Serialize(new HsmRequests.TranslatePinBlock("CHANNEL", "BANK", "1A2B3C4D5E6F7A8B", HsmClient.AccountNumber("4111111111111111"))));
        Assert.Equal((string)Golden["translate-pinblock-long-pan"]!, HsmClient.Serialize(new HsmRequests.TranslatePinBlock("CHANNEL", "BANK", "1A2B3C4D5E6F7A8B", HsmClient.AccountNumber("6220123456789012345"))));
        Assert.Equal((string)Golden["translate-pinblock-short-pan"]!, HsmClient.Serialize(new HsmRequests.TranslatePinBlock("CHANNEL", "BANK", "1A2B3C4D5E6F7A8B", HsmClient.AccountNumber("123456789012"))));
        Assert.Equal((string)Golden["translate-pinblock-terminal"]!, HsmClient.Serialize(new HsmRequests.TranslateTerminalPinBlock("TERM0001", "BANK", "1A2B3C4D5E6F7A8B", HsmClient.AccountNumber("4111111111111111"))));
    }

    [Fact]
    public async Task Interface_translates_pin_through_simcore_hsm()
    {
        await using CoreSim core = await CoreSim.StartAsync(Options(new SimHsmOptions { TranslatedPinBlock = "FFEEDDCCBBAA9988" }), cancellationToken: Ct);
        await using var app = await SimInterfaceHost.StartAsync<PinInterface>(core, services: s => s.AddSyncNetHsm());

        var request = new CoreRequest
        {
            MessageType = "0200", TranType = TranType.Payment, TraceNumber = "000001", TransactionDateTime = "0930101010",
            TerminalId = "TERM0001", Pan = "4111111111111111", Security = new Security { PinData = "1A2B3C4D5E6F7A8B" },
        };
        SimResult result = await core.SendAsync("BANK", request, cancellationToken: Ct);

        Assert.Equal("00", result.Response!.ResponseCode);
        Assert.Equal("FFEEDDCCBBAA9988", result.Response.Security?.PinData);
        SimHsmRequest hsm = Assert.Single(core.HsmRequests);
        Assert.Equal(HsmPaths.TranslatePinBlock, hsm.Path);
        Assert.Equal("111111111111", (string)JObject.Parse(hsm.Body)["account_number"]!);
    }

    [Fact]
    public async Task Key_operations_return_stub_values()
    {
        await using CoreSim core = await CoreSim.StartAsync(Options(), cancellationToken: Ct);
        await using var app = await SimInterfaceHost.StartAsync<NoopInterface>(core, services: s => s.AddSyncNetHsm());
        var hsm = app.Services.GetRequiredService<IHsmClient>();

        HsmKeyResult node = await hsm.GenerateNodeKeyAsync("BANK", Ct);
        HsmKeyResult terminal = await hsm.GenerateTerminalKeyAsync("TERM0001", Ct);
        HsmKeyResult translated = await hsm.TranslateKeyAsync("BANK", "U0123", Ct);

        Assert.True(node.IsSuccess);
        Assert.Equal("U1234567890ABCDEF1234567890ABCDEF", node.KeyUnderZmk);
        Assert.Equal("UFEDCBA0987654321FEDCBA0987654321", terminal.KeyUnderTmk);
        Assert.Equal("0A1B2C", translated.KeyCheckValue);
    }

    [Fact]
    public async Task Hsm_error_code_is_passed_through()
    {
        await using CoreSim core = await CoreSim.StartAsync(Options(new SimHsmOptions { ResponseCode = "15" }), cancellationToken: Ct);
        await using var app = await SimInterfaceHost.StartAsync<NoopInterface>(core, services: s => s.AddSyncNetHsm());

        HsmPinResult result = await app.Services.GetRequiredService<IHsmClient>().TranslatePinBlockAsync("A", "B", "1A2B3C4D5E6F7A8B", "4111111111111111", Ct);

        Assert.False(result.IsSuccess);
        Assert.Equal("15", result.ResponseCode);
    }

    [Fact]
    public async Task Unreachable_hsm_returns_96_instead_of_throwing()
    {
        int port = FreePort();
        await using CoreSim core = await CoreSim.StartAsync(Options(), cancellationToken: Ct);
        await using var app = await SimInterfaceHost.StartAsync<NoopInterface>(core, services: s => s.AddSyncNetHsm(o => o.Url = $"http://127.0.0.1:{port}"));

        HsmKeyResult result = await app.Services.GetRequiredService<IHsmClient>().GenerateNodeKeyAsync("BANK", Ct);

        Assert.Equal(HsmResult.Failed, result.ResponseCode);
        Assert.NotNull(result.ResponseMessage);
    }

    [Fact]
    public async Task Slow_hsm_times_out_with_96()
    {
        await using CoreSim core = await CoreSim.StartAsync(Options(new SimHsmOptions { DelayMs = 2000 }), cancellationToken: Ct);
        await using var app = await SimInterfaceHost.StartAsync<NoopInterface>(core, services: s => s.AddSyncNetHsm(o => o.Timeout = TimeSpan.FromMilliseconds(200)));

        HsmPinResult result = await app.Services.GetRequiredService<IHsmClient>().TranslatePinBlockAsync("A", "B", "1A2B3C4D5E6F7A8B", null, Ct);

        Assert.Equal(HsmResult.Failed, result.ResponseCode);
        Assert.Contains("timeout", result.ResponseMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Missing_url_is_a_clear_configuration_error()
    {
        SimCoreOptions options = Options();
        options.Hsm = null;
        await using CoreSim core = await CoreSim.StartAsync(options, cancellationToken: Ct);
        await using var app = await SimInterfaceHost.StartAsync<NoopInterface>(core, services: s => s.AddSyncNetHsm());

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => app.Services.GetRequiredService<IHsmClient>().GenerateNodeKeyAsync("BANK", Ct));
        Assert.Contains("SyncNet:Hsm:Url", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("4111111111111111", "111111111111")]
    [InlineData("6220123456789012345", "345678901234")]
    [InlineData("123456789012", "")]
    [InlineData(null, "")]
    public void Account_number_is_12_digits_before_check_digit(string? pan, string expected) =>
        Assert.Equal(expected, HsmClient.AccountNumber(pan));

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
