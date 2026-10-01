using Microsoft.Extensions.Logging;
using SyncNet.Template.Iso;
using SyncNet.Template.Mapping;
using SyncNetPro.Contracts;
using SyncNetPro.Iso8583;
using SyncNetPro.Sdk;
using SyncNetPro.Sdk.Core;
using SyncNetPro.Sdk.Remote;
using SyncNetPro.Sdk.Tracing;

namespace SyncNet.Template;

/// <summary>
/// Interface inbound: pengirim (bank/EDC/switch) terhubung ke port TCP interface (koneksi peran server) dan mengirim
/// ISO 8583 → interface meneruskan ke Core (kanal source / <c>port_in</c>) → respons Core dibalas sebagai ISO 8583.
/// Framing TCP (header panjang) dan trace koneksi ditangani SDK.
/// </summary>
public sealed class AcquirerInterface(ToCore toCore, ToIso toIso) : SyncNetInterface
{
    public override async Task OnRemoteMessageAsync(RemoteMessageContext context, CancellationToken cancellationToken)
    {
        if (!IsoMessage.TryParse(AcquirerIsoSpec.Instance, context.Message, out IsoMessage? iso, out string? error))
        {
            context.Logger.LogWarning("Pesan dari {Remote} tidak valid: {Error}", context.RemoteAddress, error);
            return;
        }

        context.Trace.Message(context.Node.Name, TraceDirection.Incoming, iso!.Mti, iso.FormatTrace(), context.RemoteAddress);
        IsoMessage reply = await HandleAsync(context, iso, cancellationToken);
        context.Trace.Message(context.Node.Name, TraceDirection.Outgoing, reply.Mti, reply.FormatTrace(), context.RemoteAddress);
        await context.ReplyAsync(reply.Pack(), cancellationToken);
    }

    private async Task<IsoMessage> HandleAsync(RemoteMessageContext context, IsoMessage iso, CancellationToken cancellationToken)
    {
        // Network management (sign-on, echo, key exchange): dijawab interface, tidak ke Core.
        // TODO(5): validasi sign-on / key exchange bila diperlukan.
        if (iso.Mti == "0800") return ToIso.Status(iso, ResponseCodes.Approved);
        if (!ToCore.IsFinancial(iso.Mti)) return ToIso.Status(iso, "12");

        CoreRequest request = toCore.Map(iso);
        if (string.IsNullOrEmpty(request.TranType)) return ToIso.Status(iso, "12");

        try
        {
            return toIso.From(iso, await context.SendToCoreAsync(request, cancellationToken));
        }
        catch (TimeoutException)
        {
            return ToIso.Status(iso, "68");
        }
        catch (DuplicateCoreRequestException)
        {
            return ToIso.Status(iso, "94");
        }
        catch (CoreUnavailableException ex)
        {
            context.Logger.LogWarning("Core tidak tersedia: {Error}", ex.Message);
            return ToIso.Status(iso, ResponseCodes.LinkDown);
        }
    }
}
