using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Newtonsoft.Json.Linq;
using SyncNetPro.Contracts;
using SyncNetPro.Sdk.Nodes;
using SyncNetPro.Sdk.Remote;
using SyncNetPro.Sdk.Tests.Infrastructure;

namespace SyncNetPro.Sdk.Tests;

/// <summary>Server HTTP sederhana untuk mensimulasikan biller REST.</summary>
internal sealed class FakeHttpBiller : IAsyncDisposable
{
    private readonly WebApplication _app;

    private FakeHttpBiller(WebApplication app) => _app = app;

    public List<(string Method, string Path, string Body, string? ContentType, string? Signature)> Requests { get; } = [];

    public string BaseUrl => _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();

    public static async Task<FakeHttpBiller> StartAsync(X509Certificate2? certificate = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(k => k.Listen(IPAddress.Loopback, 0, l =>
        {
            if (certificate is not null) l.UseHttps(certificate);
        }));
        WebApplication app = builder.Build();
        var biller = new FakeHttpBiller(app);
        app.MapMethods("/{**path}", ["GET", "POST"], async (HttpContext ctx) =>
        {
            string body = await new StreamReader(ctx.Request.Body).ReadToEndAsync();
            lock (biller.Requests)
            {
                biller.Requests.Add((ctx.Request.Method, ctx.Request.Path + ctx.Request.QueryString, body, ctx.Request.ContentType, ctx.Request.Headers["X-Signature"]));
            }

            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync("{\"rc\":\"00\",\"name\":\"BUDI\"}");
        });
        await app.StartAsync();
        return biller;
    }

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();
}

public class RemoteHttpTests
{
    private static RemoteConnectionInfo HttpServer(string node, string method = "POST") => new()
    {
        Name = node + "_HTTP",
        NodeName = node,
        Protocol = ConnectionProtocol.WebService,
        Role = ConnectionRole.Server,
        WsUrl = "http://127.0.0.1:0",
        WsMethod = method,
    };

    private static RemoteConnectionInfo HttpClient(string node, string url) => new()
    {
        Name = node + "_HTTP",
        NodeName = node,
        Protocol = ConnectionProtocol.WebService,
        Role = ConnectionRole.Client,
        WsUrl = url,
        WsMethod = "POST",
    };

    private static string ServerUrl(TestHost<ScriptedInterface> app, string node) =>
        ((HttpServerConnection)app.Services.GetRequiredService<IRemoteRegistry>().GetNode(node).GetConnection(node + "_HTTP")).Addresses.First();

    [Fact]
    public async Task Http_server_forwards_to_core_and_replies_without_context_cache()
    {
        await using FakeCore core = await FakeCore.StartAsync();
        var source = new MutableNodeSource { Nodes = [Messages.Node("CHANNEL", NodeCategory.Merchant, core)], Connections = [HttpServer("CHANNEL")] };
        await using var app = await TestHost<ScriptedInterface>.StartAsync([], services: s => s.AddSingleton<INodeConfigurationSource>(source));
        app.Handler.OnHttp = async ctx =>
        {
            var body = JObject.Parse(ctx.Body);
            CoreResponse rsp = await ctx.SendToCoreAsync(Messages.Request((string)body["trace"]!));
            return HttpReply.Json(new { path = ctx.Path, rc = rsp.ResponseCode, trace = rsp.TraceNumber });
        };
        await Wait.UntilAsync(() => core.SourceConnected, "kanal inbound terkoneksi");

        using var http = new System.Net.Http.HttpClient();
        using HttpResponseMessage response = await http.PostAsync(ServerUrl(app, "CHANNEL") + "/payment",
            new StringContent("{\"trace\":\"000321\"}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JObject.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("/payment", (string?)json["path"]);
        Assert.Equal("00", (string?)json["rc"]);
        Assert.Equal("000321", (string?)json["trace"]);
        Assert.True(core.SourceRequests.TryDequeue(out CoreRequest? sent));
        Assert.Equal("CHANNEL_HTTP", sent.PrivateData!.ConnectionName);
        Assert.Equal("127.0.0.1", sent.PrivateData.IpExternal);
    }

    [Fact]
    public async Task Http_server_times_out_with_408_and_rejects_other_methods()
    {
        await using FakeCore core = await FakeCore.StartAsync();
        var source = new MutableNodeSource { Nodes = [Messages.Node("CHANNEL", NodeCategory.Merchant, core, timeoutSeconds: 1)], Connections = [HttpServer("CHANNEL")] };
        await using var app = await TestHost<ScriptedInterface>.StartAsync([], services: s => s.AddSingleton<INodeConfigurationSource>(source));
        app.Handler.OnHttp = async ctx =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None);
            return HttpReply.Text("late");
        };
        using var http = new System.Net.Http.HttpClient();

        using HttpResponseMessage slow = await http.PostAsync(ServerUrl(app, "CHANNEL") + "/x", new StringContent("{}"));
        using HttpResponseMessage get = await http.GetAsync(ServerUrl(app, "CHANNEL") + "/x");

        Assert.Equal(HttpStatusCode.RequestTimeout, slow.StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, get.StatusCode);
    }

    [Fact]
    public async Task Default_handler_answers_501()
    {
        await using FakeCore core = await FakeCore.StartAsync();
        var source = new MutableNodeSource { Nodes = [Messages.Node("CHANNEL", NodeCategory.Merchant, core)], Connections = [HttpServer("CHANNEL")] };
        await using var app = await TestHost<ScriptedInterface>.StartAsync([], services: s => s.AddSingleton<INodeConfigurationSource>(source));
        using var http = new System.Net.Http.HttpClient();

        using HttpResponseMessage response = await http.PostAsync(ServerUrl(app, "CHANNEL") + "/x", new StringContent("{}"));

        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
    }

    [Fact]
    public async Task Http_client_sends_to_biller_and_maps_response()
    {
        await using FakeCore core = await FakeCore.StartAsync();
        await using FakeHttpBiller biller = await FakeHttpBiller.StartAsync();
        var source = new MutableNodeSource
        {
            Nodes = [Messages.Node("BILLER", NodeCategory.BillerIssuer, core)],
            Connections = [HttpClient("BILLER", biller.BaseUrl + "/api/")],
        };
        await using var app = await TestHost<ScriptedInterface>.StartAsync([], services: s => s.AddSingleton<INodeConfigurationSource>(source));
        app.Handler.OnRequest = async ctx =>
        {
            RemoteHttpResponse rsp = await ctx.Remote.Http.SendAsync(RemoteHttpRequest.Json("/bill/inquiry",
                new { trace = ctx.Request.TraceNumber }, new Dictionary<string, string> { ["X-Signature"] = "abc" }));
            JObject body = rsp.ReadJson<JObject>()!;
            return ctx.Request.ToResponse((string)body["rc"]!).SetAdditionalData("customer_name", (string?)body["name"]);
        };
        await Wait.UntilAsync(() => core.SinkConnected, "kanal outbound terkoneksi");

        await core.SendToInterfaceAsync(Messages.Request());
        CoreResponse response = await core.ReceiveResponseAsync();

        Assert.Equal("00", response.ResponseCode);
        Assert.True(response.TryGetAdditionalData("customer_name", out string? name));
        Assert.Equal("BUDI", name);
        var request = Assert.Single(biller.Requests);
        Assert.Equal("POST", request.Method);
        Assert.Equal("/api/bill/inquiry", request.Path);
        Assert.Equal("{\"trace\":\"000123\"}", request.Body);
        Assert.StartsWith("application/json", request.ContentType, StringComparison.Ordinal);
        Assert.Equal("abc", request.Signature);
    }

    [Theory]
    [InlineData("http://h/api", null, "http://h/api")]
    [InlineData("http://h/api", "/bill", "http://h/api/bill")]
    [InlineData("http://h/api/", "bill?x=1", "http://h/api/bill?x=1")]
    [InlineData("http://h/api", "?x=1", "http://h/api?x=1")]
    [InlineData("http://h/api", "https://other/p", "https://other/p")]
    public void Builds_url_from_ws_url_and_path(string baseUrl, string? path, string expected) =>
        Assert.Equal(new Uri(expected), HttpClientConnection.BuildUrl(baseUrl, path));

    private static X509Certificate2 SelfSigned()
    {
        using RSA rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var san = new SubjectAlternativeNameBuilder();
        san.AddIpAddress(IPAddress.Loopback);
        san.AddDnsName("localhost");
        request.CertificateExtensions.Add(san.Build());
        using X509Certificate2 cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        return X509CertificateLoader.LoadPkcs12(cert.Export(X509ContentType.Pfx), null);
    }

    [Fact]
    public async Task Untrusted_tls_certificate_is_rejected_by_default()
    {
        // Perbaikan B6: SDK lama menerima semua sertifikat.
        using X509Certificate2 certificate = SelfSigned();
        await using FakeHttpBiller biller = await FakeHttpBiller.StartAsync(certificate);
        var info = HttpClient("BILLER", biller.BaseUrl);
        var node = new NodeInfo { Name = "BILLER", RequestTimeoutSeconds = 5 };

        await using var strict = new HttpClientConnection(info, node, allowUntrustedCertificates: false, TimeSpan.FromSeconds(5), NullLogger.Instance);
        await Assert.ThrowsAsync<RemoteUnavailableException>(() => strict.SendAsync(RemoteHttpRequest.Json("/x", new { })));

        await using var relaxed = new HttpClientConnection(info, node, allowUntrustedCertificates: true, TimeSpan.FromSeconds(5), NullLogger.Instance);
        RemoteHttpResponse ok = await relaxed.SendAsync(RemoteHttpRequest.Json("/x", new { }));
        Assert.True(ok.IsSuccess);
    }

    [Fact]
    public async Task Connect_timeout_is_unavailable_not_response_timeout()
    {
        // Gagal connect = request belum terkirim (bukan kandidat reversal) → RemoteUnavailableException,
        // bukan TimeoutException "tidak ada balasan". Alamat non-routable memaksa connect timeout (seperti port tertutup di Windows).
        var info = HttpClient("BILLER", "http://10.255.255.1:81/api");
        await using var connection = new HttpClientConnection(info, new NodeInfo { Name = "BILLER" }, false, TimeSpan.FromMilliseconds(300), NullLogger.Instance);

        await Assert.ThrowsAsync<RemoteUnavailableException>(() => connection.SendAsync(new RemoteHttpRequest { Path = "/x", Body = "{}" }));
    }

    [Fact]
    public async Task Silent_biller_is_a_response_timeout()
    {
        // Koneksi terbentuk tetapi tidak ada balasan → TimeoutException (request mungkin sudah diproses biller).
        using var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var info = HttpClient("BILLER", $"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/api");
        await using var connection = new HttpClientConnection(info, new NodeInfo { Name = "BILLER" }, false, TimeSpan.FromSeconds(5), NullLogger.Instance);

        await Assert.ThrowsAsync<TimeoutException>(() =>
            connection.SendAsync(new RemoteHttpRequest { Path = "/x", Body = "{}", Timeout = TimeSpan.FromMilliseconds(300) }));
    }

    [Fact]
    public async Task Http_client_reports_unreachable_biller()
    {
        var info = HttpClient("BILLER", "http://127.0.0.1:1/api");
        await using var connection = new HttpClientConnection(info, new NodeInfo { Name = "BILLER" }, false, TimeSpan.FromSeconds(2), NullLogger.Instance);

        await Assert.ThrowsAsync<RemoteUnavailableException>(() => connection.SendAsync(new RemoteHttpRequest { Path = "/x", Body = "{}" }));
    }
}

public class NodeTimerTests
{
    [Fact]
    public async Task Echo_and_key_exchange_timers_follow_node_configuration()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        await using FakeCore core = await FakeCore.StartAsync();
        var source = new MutableNodeSource
        {
            Nodes = [Messages.Node("BILLER", NodeCategory.BillerIssuer, core) with { EchoTimerMinutes = 1, KeyChangeTimerMinutes = 2 }],
            Connections =
            [
                new RemoteConnectionInfo
                {
                    Name = "BILLER_HTTP", NodeName = "BILLER", Protocol = ConnectionProtocol.WebService, Role = ConnectionRole.Client, WsUrl = "http://127.0.0.1:1",
                },
            ],
        };
        await using var app = await TestHost<ScriptedInterface>.StartAsync([],
            services: s => s.AddSingleton<INodeConfigurationSource>(source).AddSingleton<TimeProvider>(time));

        time.Advance(TimeSpan.FromMinutes(1));
        await Wait.UntilAsync(() => app.Handler.Echoes == 1, "echo pertama");
        Assert.Equal(0, app.Handler.KeyExchanges);

        time.Advance(TimeSpan.FromMinutes(1));
        await Wait.UntilAsync(() => app.Handler.Echoes == 2 && app.Handler.KeyExchanges == 1, "echo kedua dan key exchange");
    }

    [Fact]
    public async Task Nodes_without_remote_connections_have_no_timers()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        await using FakeCore core = await FakeCore.StartAsync();
        await using var app = await TestHost<ScriptedInterface>.StartAsync(
            [Messages.Node("BILLER", NodeCategory.BillerIssuer, core) with { EchoTimerMinutes = 1 }],
            services: s => s.AddSingleton<TimeProvider>(time));

        time.Advance(TimeSpan.FromMinutes(3));
        await Task.Delay(200);

        Assert.Equal(0, app.Handler.Echoes);
    }
}
