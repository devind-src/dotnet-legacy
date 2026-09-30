using System.Net;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SyncNetPro.Sdk.Transport;

namespace SyncNetPro.Sdk.Testing;

/// <summary>Stub sistem eksternal (TCP atau HTTP) yang membalas sesuai <see cref="StubReply"/>.</summary>
public sealed class RemoteStub : IAsyncDisposable
{
    private readonly TcpFrameServer? _tcp;
    private readonly WebApplication? _http;

    private RemoteStub(RemoteStubOptions options, TcpFrameServer? tcp, WebApplication? http)
    {
        Options = options;
        _tcp = tcp;
        _http = http;
    }

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
                StubReply? rule = stub.Match(payload, null);
                if (rule is null || rule.Silent) return;
                if (rule.DelayMs > 0) await Task.Delay(rule.DelayMs).ConfigureAwait(false);
                await connection.SendAsync(ReplyBytes(rule, payload)).ConfigureAwait(false);
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
                StubReply? rule = stub.Match(body, ctx.Request.Path.Value);
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

    private StubReply? Match(byte[] payload, string? path)
    {
        string text = Encoding.UTF8.GetString(payload);
        string hex = Convert.ToHexString(payload);
        return Options.Replies.FirstOrDefault(r =>
            (r.Path is null || string.Equals(r.Path, path, StringComparison.OrdinalIgnoreCase))
            && (r.Contains is null || text.Contains(r.Contains, StringComparison.Ordinal))
            && (r.HexPrefix is null || hex.StartsWith(r.HexPrefix, StringComparison.OrdinalIgnoreCase)));
    }

    private static byte[] ReplyBytes(StubReply rule, byte[] request) =>
        rule.Echo ? request
        : rule.ReplyHex is not null ? Convert.FromHexString(rule.ReplyHex)
        : Encoding.UTF8.GetBytes(rule.Reply ?? string.Empty);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_tcp is not null) await _tcp.DisposeAsync().ConfigureAwait(false);
        if (_http is not null) await _http.DisposeAsync().ConfigureAwait(false);
    }
}
