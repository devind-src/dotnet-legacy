using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json.Linq;
using SyncNetPro.Contracts;
using SyncNetPro.Sdk.Nodes;
using SyncNetPro.Sdk.Testing;

namespace SyncNet.Template.Tests;

/// <summary>
/// End-to-end tanpa Core: test bertindak sebagai aplikasi channel (POST ke interface), SimCore (in-process) bertindak
/// sebagai Core dan membalas sesuai <c>Responder</c> di <c>simcore/simcore.json</c>.
/// </summary>
public sealed class InterfaceFlowTests : IAsyncLifetime
{
    private const string ApiKey = "test-key";
    private SimCore? _core;
    private IAsyncDisposable? _app;
    private readonly HttpClient _http = new();

    public async ValueTask InitializeAsync()
    {
        SimCoreOptions options = SimCoreOptions.Load(Path.Combine(AppContext.BaseDirectory, "simcore", "simcore.json"));
        options.LogServicesPort = 0;
        options.Interface = null;
        foreach (SimNodeOptions node in options.Nodes) node.PortIn = node.PortOut = 0;

        _core = await SimCore.StartAsync(options, cancellationToken: TestContext.Current.CancellationToken);
        int port = FreePort();
        RemoteConnectionInfo api = new()
        {
            Name = "TEMPLATE_NODE_API",
            NodeName = "TEMPLATE_NODE",
            Protocol = ConnectionProtocol.WebService,
            Role = ConnectionRole.Server,
            WsUrl = $"http://127.0.0.1:{port}",
            WsKey = ApiKey,
        };
        _app = await SimInterfaceHost.StartAsync<ChannelInterface>(_core, [api], services: s => s.AddChannelServices());
        _http.BaseAddress = new Uri(api.WsUrl);
        _http.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
    }

    public async ValueTask DisposeAsync()
    {
        _http.Dispose();
        if (_app is not null) await _app.DisposeAsync();
        if (_core is not null) await _core.DisposeAsync();
    }

    [Fact]
    public async Task Inquiry_is_forwarded_to_core_and_answered()
    {
        JObject reply = await PostAsync("/inquiry", Body("PLNPOST", 0));

        Assert.Equal("00", (string?)reply["rc"]);
        Assert.Equal("BUDI SANTOSO", (string?)reply["data"]!["customer_name"]);

        CoreRequest atCore = Assert.Single(_core!.Node("TEMPLATE_NODE").Received);
        Assert.Equal("0200", atCore.MessageType);
        Assert.Equal(TranType.Inquiry, atCore.TranType);
        Assert.Equal("532110000001", atCore.ToAccountNumber);
        Assert.Equal("TEMPLATE_NODE_API", atCore.PrivateData!.ConnectionName);
        Assert.Empty(_core.ContractWarnings);
    }

    [Fact]
    public async Task Core_response_code_is_returned_to_channel()
    {
        Assert.Equal("14", (string?)(await PostAsync("/inquiry", Body("UNKNOWN", 0)))["rc"]);
        Assert.Equal("61", (string?)(await PostAsync("/payment", Body("PLNPOST", 20_000_000)))["rc"]);
        Assert.Equal("00", (string?)(await PostAsync("/payment", Body("PLNPOST", 125_000)))["rc"]);
    }

    [Fact]
    public async Task Invalid_requests_are_rejected_by_interface()
    {
        Assert.Equal("X6", (string?)(await PostAsync("/unknown", Body("PLNPOST", 0), HttpStatusCode.NotFound))["rc"]);
        Assert.Equal("30", (string?)(await PostAsync("/inquiry", "{}", HttpStatusCode.BadRequest))["rc"]);

        _http.DefaultRequestHeaders.Remove("X-Api-Key");
        Assert.Equal("X8", (string?)(await PostAsync("/inquiry", Body("PLNPOST", 0), HttpStatusCode.Unauthorized))["rc"]);
        Assert.Empty(_core!.Node("TEMPLATE_NODE").Received);
    }

    private static string Body(string product, decimal amount) => new JObject
    {
        ["reference"] = "260930000123",
        ["trace_number"] = Random.Shared.Next(1, 999999).ToString("D6", System.Globalization.CultureInfo.InvariantCulture),
        ["terminal_id"] = "TERM0001",
        ["merchant_id"] = "MERCHANT01",
        ["product_code"] = product,
        ["customer_id"] = "532110000001",
        ["amount"] = amount,
    }.ToString();

    private async Task<JObject> PostAsync(string path, string body, HttpStatusCode expected = HttpStatusCode.OK)
    {
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await _http.PostAsync(path, content, TestContext.Current.CancellationToken);
        Assert.Equal(expected, response.StatusCode);
        return JObject.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
