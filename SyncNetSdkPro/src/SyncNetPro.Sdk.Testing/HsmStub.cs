using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace SyncNetPro.Sdk.Testing;

/// <summary>SyncNetHsm tiruan (<c>SimCoreOptions.Hsm</c>).</summary>
public sealed class SimHsmOptions
{
    /// <summary>Port HTTP (0 = acak).</summary>
    public int Port { get; set; }

    /// <summary><c>resp_code</c> semua balasan (mis. <c>96</c> untuk uji HSM gagal).</summary>
    public string ResponseCode { get; set; } = "00";

    /// <summary>PIN block hasil translate; <c>null</c> = sama dengan PIN block masukan.</summary>
    public string? TranslatedPinBlock { get; set; }

    /// <summary>Key hasil generate key node.</summary>
    public string KeyUnderZmk { get; set; } = "U1234567890ABCDEF1234567890ABCDEF";

    /// <summary>Key hasil generate key terminal.</summary>
    public string KeyUnderTmk { get; set; } = "UFEDCBA0987654321FEDCBA0987654321";

    /// <summary>KCV semua balasan key.</summary>
    public string KeyCheckValue { get; set; } = "0A1B2C";

    /// <summary>Jeda balasan (uji timeout).</summary>
    public int DelayMs { get; set; }
}

/// <summary>Request yang diterima HSM tiruan.</summary>
/// <param name="Path">Path, mis. <c>/hsm/translate-pinblock</c>.</param>
/// <param name="Body">JSON request.</param>
public sealed record SimHsmRequest(string Path, string Body);

/// <summary>Endpoint tiruan SyncNetHsm dengan balasan deterministik.</summary>
internal sealed class HsmStub : IAsyncDisposable
{
    private readonly WebApplication _app;

    private HsmStub(WebApplication app) => _app = app;

    public ConcurrentQueue<SimHsmRequest> Requests { get; } = new();

    public int Port { get; private set; }

    public static async Task<HsmStub> StartAsync(SimHsmOptions options, IPAddress bind, ILogger logger, CancellationToken cancellationToken)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls($"http://{bind}:{options.Port}");
        WebApplication app = builder.Build();
        var stub = new HsmStub(app);

        app.MapPost("/hsm/{action}", async (HttpContext ctx, string action) =>
        {
            using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8);
            string body = await reader.ReadToEndAsync(ctx.RequestAborted).ConfigureAwait(false);
            stub.Requests.Enqueue(new SimHsmRequest(ctx.Request.Path.Value ?? string.Empty, body));
            if (options.DelayMs > 0) await Task.Delay(options.DelayMs, ctx.RequestAborted).ConfigureAwait(false);

            JObject request = JObject.Parse(body);
            var reply = new JObject { ["resp_code"] = options.ResponseCode, ["resp_message"] = options.ResponseCode == "00" ? "Success" : "Failed" };
            switch (action)
            {
                case "generate-key":
                    reply["key_under_zmk"] = options.KeyUnderZmk;
                    reply["key_check_value"] = options.KeyCheckValue;
                    break;
                case "generate-key-terminal":
                    reply["key_under_tmk"] = options.KeyUnderTmk;
                    reply["key_check_value"] = options.KeyCheckValue;
                    break;
                case "translate-key":
                    reply["key_check_value"] = options.KeyCheckValue;
                    break;
                case "translate-pinblock":
                case "translate-pinblock-terminal":
                    reply["dest_pinblock"] = options.TranslatedPinBlock ?? (string?)request["source_pinblock"];
                    break;
                default:
                    return Results.NotFound();
            }

            return Results.Text(reply.ToString(Newtonsoft.Json.Formatting.None), "application/json");
        });

        await app.StartAsync(cancellationToken).ConfigureAwait(false);
        stub.Port = new Uri(app.Urls.First()).Port;
        logger.LogInformation("HSM tiruan di port {Port}", stub.Port);
        return stub;
    }

    public ValueTask DisposeAsync() => _app.DisposeAsync();
}
