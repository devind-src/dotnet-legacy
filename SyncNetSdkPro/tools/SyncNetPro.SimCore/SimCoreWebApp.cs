using System.Reflection;
using System.Text;
using System.Threading.Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using SyncNetPro.Contracts;
using SyncNetPro.Sdk.Testing;

namespace SyncNetPro.SimCore;

/// <summary>Web API + UI SimCore.</summary>
public static class SimCoreWebApp
{
    private static readonly JsonSerializerSettings Json = new()
    {
        Converters = { new StringEnumConverter() },
        ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
        {
            NamingStrategy = new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy { ProcessDictionaryKeys = false },
        },
    };

    /// <summary>Membangun aplikasi web untuk instance SimCore yang sudah berjalan.</summary>
    /// <param name="core">SimCore.</param>
    /// <param name="scenarioPath">Folder/file skenario (opsional).</param>
    /// <param name="urls">Alamat web (mis. <c>http://127.0.0.1:5080</c>).</param>
    public static WebApplication Build(Sdk.Testing.SimCore core, string? scenarioPath, string urls)
    {
        ArgumentNullException.ThrowIfNull(core);
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls(urls);
        builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Warning);
        WebApplication app = builder.Build();
        var runner = new ScenarioRunner(core);

        app.MapGet("/", () => Results.Content(ReadIndex(), "text/html; charset=utf-8"));

        app.MapGet("/api/status", () => Ok(new
        {
            core.Options.AppName,
            nodes = core.Nodes.Select(n => new
            {
                n.Name,
                category = n.Options.Category.ToString(),
                n.SinkPort,
                n.SourcePort,
                n.SinkConnected,
                n.SourceConnected,
                received = n.Received.Count,
            }),
            logServicesPort = core.LogServicesPort,
            stubs = core.RemoteStubs.Select(s => new { s.Options.Name, type = s.Options.Type.ToString(), s.Port, requests = s.RequestCount }),
            commandTarget = core.Options.Interface,
            contractWarnings = core.ContractWarnings.Count,
        }));

        app.MapGet("/api/messages", (int? limit) => Ok(core.Messages.TakeLast(limit ?? 200)));
        app.MapGet("/api/traces", (int? limit) => Ok(core.Traces.TakeLast(limit ?? 200)));

        app.MapGet("/api/scenarios", () =>
            Ok(scenarioPath is null ? [] : ScenarioRunner.Load(scenarioPath).Select(s => new { s.Name, s.Node, s.Command, request = s.Request })));

        app.MapPost("/api/scenarios/run", async (HttpContext ctx) =>
        {
            JObject body = await ReadBodyAsync(ctx).ConfigureAwait(false);
            string? name = (string?)body["name"];
            if (scenarioPath is null) return Problem("Folder skenario tidak dikonfigurasi (--scenarios).");
            var scenarios = ScenarioRunner.Load(scenarioPath).Where(s => name is null || s.Name == name).ToList();
            if (scenarios.Count == 0) return Problem($"Skenario '{name}' tidak ditemukan.", 404);
            return Ok(await runner.RunAsync(scenarios, ctx.RequestAborted).ConfigureAwait(false));
        });

        app.MapPost("/api/send", async (HttpContext ctx) =>
        {
            JObject body = await ReadBodyAsync(ctx).ConfigureAwait(false);
            if (body["request"] is not JObject request) return Problem("Body wajib berisi 'request' (JSON CoreRequest).");
            string? node = (string?)body["node"] ?? core.Nodes.FirstOrDefault()?.Name;
            if (node is null) return Problem("SimCore tidak memiliki node.");
            TimeSpan? timeout = (int?)body["timeoutMs"] is int ms ? TimeSpan.FromMilliseconds(ms) : null;

            SimResult result = await core.SendAsync(node, runner.BuildRequest(request), timeout, ctx.RequestAborted).ConfigureAwait(false);
            return Ok(new
            {
                outcome = result.Outcome,
                elapsedMs = (int)result.Elapsed.TotalMilliseconds,
                result.Warnings,
                response = result.Response is null ? null : JObject.Parse(CoreMessageCodec.Default.Serializer.Serialize(result.Response)),
            });
        });

        app.MapPost("/api/command", async (HttpContext ctx) =>
        {
            JObject body = await ReadBodyAsync(ctx).ConfigureAwait(false);
            try
            {
                return Ok(new { reply = await core.CommandAsync((string?)body["text"] ?? string.Empty, ctx.RequestAborted).ConfigureAwait(false) });
            }
            catch (Exception ex) when (ex is InvalidOperationException or IOException or System.Net.Sockets.SocketException or OperationCanceledException)
            {
                return Problem(ex.Message);
            }
        });

        app.MapGet("/api/events", async (HttpContext ctx) =>
        {
            ctx.Response.ContentType = "text/event-stream";
            ctx.Response.Headers.CacheControl = "no-cache";
            var events = Channel.CreateBounded<string>(new BoundedChannelOptions(1000) { FullMode = BoundedChannelFullMode.DropOldest });
            void OnMessage(SimMessage m) => events.Writer.TryWrite("event: message\ndata: " + JsonConvert.SerializeObject(m, Json) + "\n\n");
            void OnTrace(SimTrace t) => events.Writer.TryWrite("event: trace\ndata: " + JsonConvert.SerializeObject(t, Json) + "\n\n");
            core.MessageLogged += OnMessage;
            core.TraceReceived += OnTrace;
            try
            {
                await ctx.Response.WriteAsync(": connected\n\n", ctx.RequestAborted).ConfigureAwait(false);
                await ctx.Response.Body.FlushAsync(ctx.RequestAborted).ConfigureAwait(false);
                await foreach (string item in events.Reader.ReadAllAsync(ctx.RequestAborted).ConfigureAwait(false))
                {
                    await ctx.Response.WriteAsync(item, ctx.RequestAborted).ConfigureAwait(false);
                    await ctx.Response.Body.FlushAsync(ctx.RequestAborted).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // klien menutup koneksi
            }
            finally
            {
                core.MessageLogged -= OnMessage;
                core.TraceReceived -= OnTrace;
            }
        });

        return app;
    }

    private static IResult Ok(object value) => Results.Content(JsonConvert.SerializeObject(value, Json), "application/json; charset=utf-8");

    private static IResult Problem(string message, int status = 400) =>
        Results.Content(JsonConvert.SerializeObject(new { error = message }, Json), "application/json; charset=utf-8", Encoding.UTF8, status);

    private static async Task<JObject> ReadBodyAsync(HttpContext ctx)
    {
        using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8);
        string text = await reader.ReadToEndAsync(ctx.RequestAborted).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(text) ? [] : JObject.Parse(text);
    }

    private static string ReadIndex()
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("simcore.index.html")
            ?? throw new InvalidOperationException("UI tidak ditemukan.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
