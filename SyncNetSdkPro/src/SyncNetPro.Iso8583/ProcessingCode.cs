using SyncNetPro.Contracts;

namespace SyncNetPro.Iso8583;

/// <summary>
/// Pemetaan processing code (field 3) ↔ <see cref="TranType"/>, sama dengan <c>IsoConverter</c> SDK lama.
/// </summary>
public static class ProcessingCode
{
    /// <summary>Tran type dari MTI + processing code (MTI advice/reversal diprioritaskan).</summary>
    public static string ToTranType(string? mti, string? processingCode) => mti switch
    {
        "0120" or "0121" or "0130" or "0131" or "0220" or "0221" or "0230" or "0231" => TranType.Advice,
        "0400" or "0401" or "0410" or "0411" or "0420" or "0421" or "0430" or "0431" => TranType.Reversal,
        _ => ToTranType(processingCode),
    };

    /// <summary>Tran type dari 2 digit awal processing code 6 digit; kosong bila tidak dikenal.</summary>
    public static string ToTranType(string? processingCode)
    {
        if (processingCode is not { Length: 6 } || !int.TryParse(processingCode.AsSpan(0, 2), out int code)) return string.Empty;

        return code switch
        {
            0 => TranType.Purchase,
            >= 1 and <= 19 => TranType.Withdrawal,
            20 => TranType.Refund,
            >= 21 and <= 29 => TranType.Deposit,
            >= 30 and <= 34 => TranType.BalanceInquiry,
            35 => TranType.MiniStatement,
            >= 36 and <= 39 => TranType.Inquiry,
            >= 40 and <= 49 => TranType.Transfer,
            >= 50 and <= 59 => TranType.Payment,
            90 or 91 => TranType.Admin,
            92 => TranType.PinChange,
            93 => TranType.KeyChange,
            94 => TranType.VirtualTopUp,
            95 => TranType.VirtualAdjust,
            96 => TranType.VirtualBalance,
            _ => string.Empty,
        };
    }

    /// <summary>
    /// Processing code 6 digit: kode transaksi (atau <paramref name="tranTypeExt"/> bila 2 karakter) + jenis rekening asal + tujuan.
    /// Jenis rekening kosong menjadi <c>00</c> (SDK lama menghasilkan kode 2–4 digit yang tidak valid).
    /// </summary>
    public static string FromTranType(string? tranType, string? tranTypeExt = null, string? fromAccType = null, string? toAccType = null)
    {
        string code = tranTypeExt is { Length: 2 } ? tranTypeExt : tranType switch
        {
            TranType.BalanceInquiry => "31",
            TranType.MiniStatement => "35",
            TranType.Inquiry => "38",
            TranType.Withdrawal => "01",
            TranType.Payment or TranType.Advice or TranType.Reversal => "50",
            TranType.Purchase => "00",
            TranType.Transfer => "41",
            TranType.Refund => "20",
            TranType.Deposit => "21",
            TranType.Admin => "91",
            TranType.PinChange => "92",
            TranType.KeyChange => "93",
            TranType.VirtualTopUp => "94",
            TranType.VirtualAdjust => "95",
            TranType.VirtualBalance => "96",
            _ => "99",
        };

        return code + AccountType(fromAccType) + AccountType(toAccType);
    }

    /// <summary>Jenis rekening asal (digit 3–4), default <c>00</c>.</summary>
    public static string FromAccountType(string? processingCode) => processingCode is { Length: 6 } ? processingCode.Substring(2, 2) : "00";

    /// <summary>Jenis rekening tujuan (digit 5–6), default <c>00</c>.</summary>
    public static string ToAccountType(string? processingCode) => processingCode is { Length: 6 } ? processingCode.Substring(4, 2) : "00";

    private static string AccountType(string? value) => value is { Length: 2 } ? value : "00";
}
