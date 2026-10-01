using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SyncNet.Template.Iso;
using SyncNet.Template.Mapping;
using SyncNetPro.Contracts;
using SyncNetPro.Iso8583;
using SyncNetPro.Sdk;
using SyncNetPro.Sdk.Nodes;
using SyncNetPro.Sdk.Remote;
using SyncNetPro.Sdk.Tracing;

namespace SyncNet.Template;

/// <summary>
/// Interface outbound: menerima request dari Core (kanal sink / port_out), meneruskan ke biller via TCP ISO 8583,
/// lalu membalas Core. Korelasi, timeout, framing TCP, trace, dan reconnect ditangani SDK.
/// </summary>
public sealed class BillerInterface(ToRemote toRemote, ToCore toCore, IOptions<BillerOptions> options) : SyncNetInterface
{
    public override async Task<CoreResponse?> OnCoreRequestAsync(CoreRequestContext context, CancellationToken cancellationToken)
    {
        CoreRequest request = context.Request;

        // TODO(4): tambahkan/hapus tran_type yang didukung biller ini.
        IsoMessage? iso = request.TranType switch
        {
            TranType.Inquiry => toRemote.Inquiry(request),
            TranType.Payment => toRemote.Payment(request),
            TranType.Advice => toRemote.Advice(request),
            TranType.Reversal => toRemote.Reversal(request),
            _ => null,
        };

        if (iso is null) return request.ToResponse(ResponseCodes.NotSupported, "Transaction is not supported", AuthorizedBy.Internal);
        if (!context.Remote.IsConnected) return request.ToResponse(ResponseCodes.LinkDown, "Link down", AuthorizedBy.Internal);

        context.Trace.Message(context.Node.Name, TraceDirection.Outgoing, iso.Mti, iso.FormatTrace());
        try
        {
            byte[] reply = await context.Remote.SendAndReceiveAsync(iso.Pack(), BillerIsoSpec.CorrelationKey(iso), cancellationToken: cancellationToken);
            context.Trace.Message(context.Node.Name, TraceDirection.Incoming, "RSP", IsoMessage.FormatTrace(BillerIsoSpec.Instance, reply));
            return toCore.From(IsoMessage.Parse(BillerIsoSpec.Instance, reply), request);
        }
        catch (RemoteUnavailableException)
        {
            return request.ToResponse(ResponseCodes.LinkDown, "Link down", AuthorizedBy.Internal);
        }
        catch (TimeoutException)
        {
            // Tidak membalas: Core menandai timeout dan (bila dikonfigurasi) mengirim reversal/advice otomatis.
            context.Logger.LogWarning("Biller tidak membalas trace {Trace}", request.TraceNumber);
            return null;
        }
    }

    /// <summary>Kunci korelasi balasan biller (harus sama dengan <see cref="BillerIsoSpec.CorrelationKey"/> request).</summary>
    public override string? GetRemoteCorrelationKey(RemoteConnectionInfo connection, ReadOnlySpan<byte> message) =>
        IsoMessage.TryParse(BillerIsoSpec.Instance, message, out IsoMessage? iso, out _) && iso!.Mti != "0810" ? BillerIsoSpec.CorrelationKey(iso) : null;

    /// <summary>Pesan dari biller yang bukan balasan transaksi (network management, balasan echo).</summary>
    public override async Task OnRemoteMessageAsync(RemoteMessageContext context, CancellationToken cancellationToken)
    {
        if (!IsoMessage.TryParse(BillerIsoSpec.Instance, context.Message, out IsoMessage? iso, out string? error))
        {
            context.Logger.LogWarning("Pesan biller tidak valid: {Error}", error);
            return;
        }

        context.Trace.Message(context.Node.Name, TraceDirection.Incoming, iso!.Mti, iso.FormatTrace(), context.RemoteAddress);
        if (iso.Mti == "0800") await context.ReplyAsync(iso.CreateResponse("00").Pack(), cancellationToken);
    }

    // TODO(5): hapus bila biller tidak memakai sign-on / echo.
    public override Task OnRemoteConnectedAsync(RemoteConnectionContext context, CancellationToken cancellationToken) =>
        options.Value.SignOn ? context.Remote.SendAsync(NetworkMessages.SignOn(options.Value.AcquirerId).Pack(), cancellationToken) : Task.CompletedTask;

    /// <summary>Interval dari <c>sw_nodes</c> (echo timer, menit).</summary>
    public override Task OnEchoTimerAsync(NodeContext context, CancellationToken cancellationToken) =>
        context.Remote.IsConnected ? context.Remote.SendAsync(NetworkMessages.Echo(options.Value.AcquirerId).Pack(), cancellationToken) : Task.CompletedTask;
}
