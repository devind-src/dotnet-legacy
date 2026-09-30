using Microsoft.Extensions.Logging;
using SyncNetPro.Contracts;
using SyncNetPro.Sdk.Nodes;

namespace SyncNetPro.Sdk;

/// <summary>
/// Base class interface. Override hanya method yang dibutuhkan — semua method punya perilaku
/// default yang aman (tidak ada lagi <c>NotImplementedException</c>).
/// </summary>
/// <remarks>
/// Pemetaan dari SDK lama (<c>IAppProcessor</c>): <c>ProcessMsgFromSinkNode</c> → <see cref="OnCoreRequestAsync"/>;
/// <c>ProcessMsgFromSourceNode</c> → hasil <c>await context.Core.SendAsync(...)</c> (+ <see cref="OnUnmatchedCoreResponseAsync"/>);
/// <c>NetworkManagement</c> → <see cref="OnNetworkCommandAsync"/>; <c>Resync</c> → <see cref="OnConfigurationReloadedAsync"/>.
/// Callback transport remote (TCP/HTTP, echo, sign-on) ditambahkan pada fase 3.
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

    /// <summary>Dipanggil setelah konfigurasi dimuat ulang (command RESYNC).</summary>
    public virtual Task OnConfigurationReloadedAsync(NodeConfiguration configuration, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Dipanggil setelah semua kanal dibuka.</summary>
    public virtual Task OnStartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Dipanggil sebelum kanal ditutup saat aplikasi berhenti.</summary>
    public virtual Task OnStoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
