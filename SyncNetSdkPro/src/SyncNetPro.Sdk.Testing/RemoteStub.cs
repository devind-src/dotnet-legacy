using System.Net;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SyncNetPro.Iso8583;
using SyncNetPro.Sdk.Transport;

namespace SyncNetPro.Sdk.Testing;

/// <summary>Stub sistem eksternal (TCP atau HTTP) yang membalas sesuai <see cref="StubReply"/>.</summary>
public sealed partial class RemoteStub : IAsyncDisposable
{
    private readonly TcpFrameServer? _tcp;
    private readonly WebApplication? _http;
    private readonly IsoSpec? _isoSpec;

    private RemoteStub(RemoteStubOptions options, TcpFrameServer? tcp, WebApplication? http)
    {
        Options = options;
        _tcp = tcp;
        _http = http;
        _isoSpec = options.Iso?.Build();
    }

    /// <summary>Request yang diterima (urut).</summary>
    public ConcurrentQueue<byte[]> Received { get; } = new();

    /// <summary>Konfigurasi.</summary>
    public RemoteStubOptions Options { get; }

    /// <summary>Jumlah request yang diterima.</summary>
    public int RequestCount => _count;

    /// <summary>Port aktual.</summary>
    public int Port => _tcp?.LocalEndPoint.Port
        ?? new Uri(_http!.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First()).Port;

    private int _count;

    /// <summary>Menjalankan stub.</summary>
    public static async Task<RemoteStub> StartAsync(RemoteStubOptions options, IPAddress bind, ILogger logger, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        RemoteStub? stub = null;

        if (options.Type == RemoteStubType.Tcp)
        {
            ITcpFrameCodec codec = options.Header is null ? NoHeaderCodec.Instance : new LengthPrefixCodec(options.Header);
            var server = new TcpFrameServer(new IPEndPoint(bind, options.Port), codec, new TcpFrameServerOptions(), logger);
            stub = new RemoteStub(options, server, null);
            server.FrameReceived = async (connection, payload) =>
            {
                Interlocked.Increment(ref stub._count);
                stub.Received.Enqueue(payload);
                IsoMessage? iso = stub.ParseIso(payload);
                StubReply? rule = stub.Match(payload, null, iso);
                if (rule is null || rule.Silent) return;
                if (rule.DelayMs > 0) await Task.Delay(rule.DelayMs).ConfigureAwait(false);
                await connection.SendAsync(iso is not null && rule.IsoSet is not null ? IsoReply(rule, iso) : ReplyBytes(rule, payload)).ConfigureAwait(false);
            };
            await server.StartAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls($"http://{bind}:{options.Port}");
            WebApplication app = builder.Build();
            stub = new RemoteStub(options, null, app);
            app.Map("/{**path}", async (HttpContext ctx) =>
            {
                Interlocked.Increment(ref stub._count);
                using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8);
                byte[] body = Encoding.UTF8.GetBytes(await reader.ReadToEndAsync().ConfigureAwait(false));
                stub.Received.Enqueue(body);
                StubReply? rule = stub.Match(body, ctx.Request.Path.Value, null);
                if (rule is null || rule.Silent)
                {
                    if (rule?.Silent == true) await Task.Delay(Timeout.Infinite, ctx.RequestAborted).ConfigureAwait(false);
                    ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }

                if (rule.DelayMs > 0) await Task.Delay(rule.DelayMs, ctx.RequestAborted).ConfigureAwait(false);
                ctx.Response.StatusCode = rule.Status;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Body.WriteAsync(ReplyBytes(rule, body)).ConfigureAwait(false);
            });
            await app.StartAsync(cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation("Stub {Name} ({Type}) di port {Port}", options.Name, options.Type, stub.Port);
        return stub;
    }

    private IsoMessage? ParseIso(byte[] payload) =>
        _isoSpec is not null && IsoMessage.TryParse(_isoSpec, payload, out IsoMessage? message, out _) ? message : null;

    private StubReply? Match(byte[] payload, string? path, IsoMessage? iso)
    {
        string text = Encoding.UTF8.GetString(payload);
        string hex = Convert.ToHexString(payload);
        return Options.Replies.FirstOrDefault(r =>
            (r.Path is null || string.Equals(r.Path, path, StringComparison.OrdinalIgnoreCase))
            && (r.Contains is null || text.Contains(r.Contains, StringComparison.Ordinal))
            && (r.HexPrefix is null || hex.StartsWith(r.HexPrefix, StringComparison.OrdinalIgnoreCase))
            && (r.Mti is null || iso?.Mti == r.Mti)
            && (r.IsoMatch is null || (iso is not null && r.IsoMatch.All(m => iso[m.Key]?.StartsWith(m.Value, StringComparison.Ordinal) == true))));
    }

    private static byte[] IsoReply(StubReply rule, IsoMessage request)
    {
        IsoMessage response = request.CreateResponse();
        foreach ((int field, string value) in rule.IsoSet!)
        {
            response[field] = FieldPlaceholder().Replace(value, m => request[int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)] ?? string.Empty);
        }

        return response.Pack();
    }

    private static byte[] ReplyBytes(StubReply rule, byte[] request)
    {
        if (rule.Echo) return request;
        if (rule.ReplyHex is not null) return Convert.FromHexString(rule.ReplyHex);
        string reply = rule.Reply ?? string.Empty;
        if (reply.Contains("{{json:", StringComparison.Ordinal))
        {
            JObject? json = null;
            try
            {
                json = JObject.Parse(Encoding.UTF8.GetString(request));
            }
            catch (Newtonsoft.Json.JsonException)
            {
                // request bukan JSON: placeholder dikosongkan
            }

            reply = JsonPlaceholder().Replace(reply, m => json?[m.Groups[1].Value]?.ToString() ?? string.Empty);
        }

        return Encoding.UTF8.GetBytes(reply);
    }

    [GeneratedRegex(@"\{\{field:(\d+)\}\}")]
    private static partial Regex FieldPlaceholder();

    [GeneratedRegex(@"\{\{json:([A-Za-z0-9_]+)\}\}")]
    private static partial Regex JsonPlaceholder();

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_tcp is not null) await _tcp.DisposeAsync().ConfigureAwait(false);
        if (_http is not null) await _http.DisposeAsync().ConfigureAwait(false);
    }
}
