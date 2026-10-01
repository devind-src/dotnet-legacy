using SyncNetPro.Contracts;
using SyncNetPro.Sdk;
using SyncNetPro.Sdk.Nodes;
using SyncNetPro.Sdk.Remote;

namespace SyncNet.Template;

/// <summary>
/// Interface kosong: override hanya event yang dibutuhkan. Default SDK: request Core dibalas <c>A1</c>
/// (not supported), request HTTP dibalas 501, event lain diabaikan. Lihat template syncnet-outbound-* /
/// syncnet-inbound-* untuk contoh lengkap.
/// </summary>
public sealed class MyInterface : SyncNetInterface
{
    // --- Outbound: Core → interface (kanal sink / port_out) ---------------------------------------------------
    // public override async Task<CoreResponse?> OnCoreRequestAsync(CoreRequestContext context, CancellationToken cancellationToken)
    // {
    //     CoreRequest request = context.Request;
    //     // HTTP: RemoteHttpResponse r = await context.Remote.Http.SendAsync(RemoteHttpRequest.Json("/path", body), cancellationToken);
    //     // TCP : byte[] r = await context.Remote.SendAndReceiveAsync(payload, correlationKey, cancellationToken: cancellationToken);
    //     return request.ToResponse(ResponseCodes.Approved, "Approved");     // null = tidak membalas (Core timeout)
    // }

    // --- Inbound: sistem eksternal → interface → Core (kanal source / port_in) --------------------------------
    // public override async Task<HttpReply> OnHttpRequestAsync(HttpRequestContext context, CancellationToken cancellationToken)
    // {
    //     CoreResponse response = await context.SendToCoreAsync(new CoreRequest { /* ... */ }, cancellationToken);
    //     return HttpReply.Json(new { rc = response.ResponseCode });
    // }

    // public override async Task OnRemoteMessageAsync(RemoteMessageContext context, CancellationToken cancellationToken)
    // {
    //     // Pesan TCP dari eksternal yang bukan balasan SendAndReceiveAsync.
    //     await context.ReplyAsync(reply, cancellationToken);
    // }

    // Kunci korelasi balasan TCP (harus sama dengan correlationKey pada SendAndReceiveAsync).
    // public override string? GetRemoteCorrelationKey(RemoteConnectionInfo connection, ReadOnlySpan<byte> message) => null;

    // --- Koneksi & timer ---------------------------------------------------------------------------------------
    // public override Task OnRemoteConnectedAsync(RemoteConnectionContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    // public override Task OnRemoteDisconnectedAsync(RemoteConnectionContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    // public override Task OnEchoTimerAsync(NodeContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    // public override Task OnKeyExchangeTimerAsync(NodeContext context, CancellationToken cancellationToken) => Task.CompletedTask;

    // --- Siklus hidup --------------------------------------------------------------------------------------------
    // public override Task OnStartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    // public override Task OnConfigurationReloadedAsync(NodeConfiguration configuration, CancellationToken cancellationToken) => Task.CompletedTask;  // RESYNC
    // public override Task OnStoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
