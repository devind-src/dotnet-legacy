using System.Globalization;
using SyncNetPro.Contracts;
using SyncNetPro.Iso8583;

namespace SyncNet.Template.Mapping;

/// <summary>ISO 8583 pengirim → CoreRequest.</summary>
public sealed class ToCore
{
    // TODO(2): MTI yang diteruskan ke Core.
    public static bool IsFinancial(string mti) => mti is "0200" or "0220" or "0221" or "0400" or "0401";

    // TODO(3): mapping field ISO → CoreRequest (format pesan ke Core tidak boleh berubah).
    public CoreRequest Map(IsoMessage iso)
    {
        string? processingCode = iso[3];
        string tranType = ProcessingCode.ToTranType(iso.Mti, processingCode);
        return new CoreRequest
        {
            MessageType = iso.Mti,
            Pan = iso[2],
            TranType = tranType,
            FromAccountType = ProcessingCode.FromAccountType(processingCode),
            ToAccountType = ProcessingCode.ToAccountType(processingCode),
            Amount = long.TryParse(iso[4], NumberStyles.None, CultureInfo.InvariantCulture, out long amount) ? amount : 0,
            TransactionDateTime = iso[7],
            TraceNumber = iso[11],
            AcquirerInstitutionId = iso[32],
            ReferenceNumber = iso[37]?.Trim(),
            TerminalId = iso[41]?.Trim(),
            MerchantId = iso[42]?.Trim(),
            Currency = iso[49],
            ToAccountNumber = iso[48]?.Trim(),
            OriginalData = tranType == TranType.Reversal ? OriginalData(iso[90]) : null,
        };
    }

    /// <summary>
    /// Field 90 (MTI asal 4 + STAN 6 + tanggal-jam 10 + ...) → <c>original_data</c> Core
    /// (tran type asal + tanggal-jam + STAN, mis. <c>PAY0930101500000122</c>).
    /// </summary>
    internal static string? OriginalData(string? field90) =>
        field90 is { Length: >= 20 } ? string.Concat(TranType.Payment, field90.AsSpan(10, 10), field90.AsSpan(4, 6)) : null;
}
