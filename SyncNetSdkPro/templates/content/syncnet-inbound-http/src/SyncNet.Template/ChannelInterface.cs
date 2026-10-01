using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SyncNet.Template.Mapping;
using SyncNet.Template.Models;
using SyncNetPro.Contracts;
using SyncNetPro.Sdk;
using SyncNetPro.Sdk.Core;
using SyncNetPro.Sdk.Remote;
using SyncNetPro.Sdk.Tracing;
#if (WithRouting)
using System.Diagnostics;
using SyncNet.Template.Routing;
#endif

namespace SyncNet.Template;

/// <summary>
/// Interface inbound: aplikasi channel memanggil REST API interface (koneksi web service peran server,
/// <c>ws_url</c> = alamat listen) → interface meneruskan ke Core (kanal source / <c>port_in</c>) → respons Core
/// dikembalikan sebagai JSON.
/// </summary>
#if (WithRouting)
public sealed class ChannelInterface(ToCore toCore, ToChannel toChannel, IOptions<ChannelOptions> options, RoutingStep? routing = null) : SyncNetInterface
#else
public sealed class ChannelInterface(ToCore toCore, ToChannel toChannel, IOptions<ChannelOptions> options) : SyncNetInterface
#endif
{
    public override async Task<HttpReply> OnHttpRequestAsync(HttpRequestContext context, CancellationToken cancellationToken)
    {
        context.Trace.Message(context.Node.Name, TraceDirection.Incoming, context.Method + " " + context.Path, context.Body, context.RemoteAddress);
        HttpReply reply = await HandleAsync(context, cancellationToken);
        context.Trace.Message(context.Node.Name, TraceDirection.Outgoing, $"RSP {(int)reply.StatusCode}", reply.Body, context.RemoteAddress);
        return reply;
    }

    private async Task<HttpReply> HandleAsync(HttpRequestContext context, CancellationToken cancellationToken)
    {
        if (!IsAuthorized(context)) return Reply(ToChannel.Status(ChannelCodes.AuthFailed, "OTENTIKASI TIDAK VALID"), HttpStatusCode.Unauthorized);
        if (context.Path.TrimEnd('/').Equals("/echo", StringComparison.OrdinalIgnoreCase)) return Reply(ToChannel.Status(ResponseCodes.Approved, "SUCCESS"));
        if (!string.Equals(context.Method, "POST", StringComparison.OrdinalIgnoreCase)) return Reply(ToChannel.Status(ChannelCodes.InvalidUrl, "URL TIDAK VALID"), HttpStatusCode.MethodNotAllowed);

        string? tranType = ToCore.TranTypeFor(context.Path);
        if (tranType is null) return Reply(ToChannel.Status(ChannelCodes.InvalidUrl, "URL TIDAK VALID"), HttpStatusCode.NotFound);

        ChannelRequest? body;
        try
        {
            body = JsonConvert.DeserializeObject<ChannelRequest>(context.Body);
        }
        catch (JsonException)
        {
            body = null;
        }

        // TODO(5): validasi tambahan (terminal terdaftar, limit nominal, duplikat, dsb.).
        if (body is null || string.IsNullOrEmpty(body.TraceNumber) || string.IsNullOrEmpty(body.TerminalId))
            return Reply(ToChannel.Status(ChannelCodes.InvalidRequest, "FORMAT REQUEST TIDAK VALID"), HttpStatusCode.BadRequest);

        CoreRequest request = toCore.Map(tranType, body);
#if (WithRouting)
        // Routing & fee (padanan ApplyFeesAndRouting ApiChannel); Jadwal Routing: tidak ada biller buka = tolak.
        if (routing is not null && !await routing.ApplyAsync(request, cancellationToken))
            return Reply(ToChannel.Status(ChannelCodes.BillerCutoff, "BILLER SEDANG CUT-OFF"));
#endif

#if (WithRouting)
        long started = Stopwatch.GetTimestamp();
#endif
        try
        {
            CoreResponse response = await context.SendToCoreAsync(request, cancellationToken);
#if (WithRouting)
            if (routing is not null) await routing.RecordResultAsync(request, response, (int)Stopwatch.GetElapsedTime(started).TotalMilliseconds, cancellationToken);
#endif
            return Reply(toChannel.From(response));
        }
        catch (TimeoutException)
        {
            // Core tidak membalas dalam request_timeout node; status akhir transaksi dicek channel lewat advice.
            return Reply(ToChannel.Status(ChannelCodes.Timeout, "TRANSAKSI TIMEOUT"));
        }
        catch (DuplicateCoreRequestException)
        {
            return Reply(ToChannel.Status("X2", "TRANSAKSI DUPLIKAT"));
        }
        catch (CoreUnavailableException ex)
        {
            context.Logger.LogWarning("Core tidak tersedia: {Error}", ex.Message);
            return Reply(ToChannel.Status(ChannelCodes.LinkDown, "LINK DOWN"));
        }
    }

    private bool IsAuthorized(HttpRequestContext context)
    {
        string? expected = context.Connection.Info.WsKey;
        if (string.IsNullOrEmpty(expected)) return true;
        return context.Headers.TryGetValue(options.Value.ApiKeyHeader, out string? key)
            && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(System.Text.Encoding.UTF8.GetBytes(key), System.Text.Encoding.UTF8.GetBytes(expected));
    }

    private static HttpReply Reply(ChannelResponse body, HttpStatusCode status = HttpStatusCode.OK) => HttpReply.Json(body, status);
}
