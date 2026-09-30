using Microsoft.Extensions.Logging;
using SyncNetPro.Contracts;
using System.Net;
using SyncNetPro.Sdk.Nodes;
using SyncNetPro.Sdk.Remote;
using SyncNetPro.Sdk.Transport;

namespace SyncNetPro.Sdk;

/// <summary>
/// Base class interface. Override hanya method yang dibutuhkan — semua method punya perilaku
/// default yang aman (tidak ada lagi <c>NotImplementedException</c>).
/// </summary>
/// <remarks>
/// Pemetaan dari SDK lama (<c>IAppProcessor</c>): <c>ProcessMsgFromSinkNode</c> → <see cref="OnCoreRequestAsync"/>;
/// <c>ProcessMsgFromSourceNode</c> → hasil <c>await context.Core.SendAsync(...)</c> (+ <see cref="OnUnmatchedCoreResponseAsync"/>);
/// <c>NetworkManagement</c> → <see cref="OnNetworkCommandAsync"/>; <c>Resync</c> → <see cref="OnConfigurationReloadedAsync"/>.
/// <c>ProcessMsgFromRemoteTcp</c> → <see cref="OnRemoteMessageAsync"/> (atau hasil <c>await context.Remote.SendAndReceiveAsync</c>);
/// <c>ProcessMsgFromRemoteWsServer</c> → <see cref="OnHttpRequestAsync"/>; <c>ProcessMsgFromRemoteWsClient</c> →
/// hasil <c>await context.Remote.Http.SendAsync</c>; <c>TimerAutoSignon/TimerEcho/TimerKeyExchange</c> →
/// <see cref="OnAutoSignOnAsync"/>/<see cref="OnEchoTimerAsync"/>/<see cref="OnKeyExchangeTimerAsync"/>.
/// </remarks>
public abstract class SyncNetInterface
{
    /// <summary>
    /// Request dari Core (kanal outbound). Kembalikan response untuk dibalas ke Core, atau <c>null</c>
    /// bila tidak ingin membalas (Core akan menangani timeout). Default: <c>A1 Transaction is not supported</c>.
    /// </summary>
    public virtual Task<CoreResponse?> OnCoreRequestAsync(CoreRequestContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Logger.LogWarning("Transaksi {TranType} dari Core tidak ditangani interface; dibalas {Code}",
            context.Request.TranType, ResponseCodes.NotSupported);
        return Task.FromResult<CoreResponse?>(
            context.Request.ToResponse(ResponseCodes.NotSupported, "Transaction is not supported", AuthorizedBy.Internal));
    }

    /// <summary>Respons Core yang tidak ditunggu lagi (terlambat setelah timeout atau tidak dikenal). Default: dicatat.</summary>
    public virtual Task OnUnmatchedCoreResponseAsync(CoreResponseContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Logger.LogWarning("Respons Core tidak ditemukan request-nya (tran {TranType}, trace {Trace}, rc {Rc})",
            context.Response.TranType, context.Response.TraceNumber, context.Response.ResponseCode);
        return Task.CompletedTask;
    }

    /// <summary>Perintah ECHO/SIGNON/SIGNOFF/KEYCHANGE/OTHER dari command port. Default: dicatat.</summary>
    public virtual Task OnNetworkCommandAsync(NetworkCommandContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Logger.LogInformation("Perintah {Command} untuk {Node} tidak ditangani interface", context.Command, context.NodeName);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Pesan TCP dari sistem eksternal yang bukan balasan request yang sedang ditunggu (request dari bank/EDC,
    /// network management, balasan terlambat). Default: dicatat.
    /// </summary>
    public virtual Task OnRemoteMessageAsync(RemoteMessageContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Logger.LogWarning("Pesan {Length} byte dari {Remote} ({Connection}) tidak ditangani",
            context.Message.Length, context.RemoteAddress, context.Connection.Info.Name);
        return Task.CompletedTask;
    }

    /// <summary>Request HTTP masuk (koneksi web service peran server). Default: 501 Not Implemented.</summary>
    public virtual Task<HttpReply> OnHttpRequestAsync(HttpRequestContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Logger.LogWarning("Request HTTP {Method} {Path} tidak ditangani interface", context.Method, context.Path);
        return Task.FromResult(HttpReply.Status(HttpStatusCode.NotImplemented));
    }

    /// <summary>
    /// Kunci korelasi dari pesan TCP masuk untuk mencocokkan balasan dengan
    /// <see cref="IRemoteTcpConnection.SendAndReceiveAsync"/>; <c>null</c> = bukan balasan.
    /// Contoh ISO 8583: 2 digit MTI + DE11 + DE41 (pola <c>ApiBillerIso</c>). Default: <c>null</c>.
    /// </summary>
    public virtual string? GetRemoteCorrelationKey(RemoteConnectionInfo connection, ReadOnlySpan<byte> message) => null;

    /// <summary>
    /// Codec framing kustom untuk koneksi TCP (mis. <c>protocol = 4</c>). <c>null</c> = pemetaan standar
    /// (<see cref="RemoteCodecs.ForConnection"/>).
    /// </summary>
    public virtual ITcpFrameCodec? CreateTcpCodec(RemoteConnectionInfo connection) => null;

    /// <summary>Koneksi TCP persistent ke sistem eksternal terbentuk.</summary>
    public virtual Task OnRemoteConnectedAsync(RemoteConnectionContext context, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Koneksi TCP persistent ke sistem eksternal terputus.</summary>
    public virtual Task OnRemoteDisconnectedAsync(RemoteConnectionContext context, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Dipanggil <see cref="RemoteOptions.AutoSignOnDelay"/> setelah terkoneksi bila <c>auto_signon = 1</c>
    /// (setara <c>TimerAutoSignon</c>). Default: dicatat.
    /// </summary>
    public virtual Task OnAutoSignOnAsync(RemoteConnectionContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Logger.LogInformation("Auto sign-on {Node} tidak diimplementasikan interface", context.Node.Name);
        return Task.CompletedTask;
    }

    /// <summary>Timer echo tiap <c>echo_timer</c> menit (setara <c>TimerEcho</c>).</summary>
    public virtual Task OnEchoTimerAsync(NodeContext context, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Timer key exchange tiap <c>keychange_timer</c> menit (setara <c>TimerKeyExchange</c>).</summary>
    public virtual Task OnKeyExchangeTimerAsync(NodeContext context, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Dipanggil setelah konfigurasi dimuat ulang (command RESYNC).</summary>
    public virtual Task OnConfigurationReloadedAsync(NodeConfiguration configuration, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Dipanggil setelah semua kanal dibuka.</summary>
    public virtual Task OnStartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Dipanggil sebelum kanal ditutup saat aplikasi berhenti.</summary>
    public virtual Task OnStoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
