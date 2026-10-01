using Microsoft.Extensions.Options;
using SyncNet.Template.Models;
using SyncNetPro.Contracts;

namespace SyncNet.Template.Mapping;

/// <summary>Request channel → CoreRequest.</summary>
public sealed class ToCore(IOptions<ChannelOptions> options, TimeProvider time)
{
    // TODO(2): path → tran_type yang didukung.
    public static string? TranTypeFor(string path) => path.TrimEnd('/').ToUpperInvariant() switch
    {
        "/INQUIRY" => TranType.Inquiry,
        "/PAYMENT" => TranType.Payment,
        "/ADVICE" => TranType.Advice,
        "/REVERSAL" => TranType.Reversal,
        _ => null,
    };

    // TODO(3): mapping field channel → CoreRequest (format pesan ke Core tidak boleh berubah).
    public CoreRequest Map(string tranType, ChannelRequest request) => new()
    {
        MessageType = tranType switch
        {
            TranType.Reversal => "0400",
            TranType.Advice => "0220",
            _ => "0200",
        },
        TranType = tranType,
        TranTypeExt = request.ProductCode,
        ReceivingInstitutionId = request.ProductCode,
        Amount = request.Amount,
        Currency = "360",
        TraceNumber = request.TraceNumber,
        TransactionDateTime = time.GetLocalNow().ToString("MMddHHmmss", System.Globalization.CultureInfo.InvariantCulture),
        TerminalId = request.TerminalId,
        MerchantId = request.MerchantId,
        AcquirerInstitutionId = options.Value.AcquirerId,
        ReferenceNumber = request.Reference,
        ToAccountNumber = request.CustomerId,
        OriginalData = request.OriginalData,
    };
}
