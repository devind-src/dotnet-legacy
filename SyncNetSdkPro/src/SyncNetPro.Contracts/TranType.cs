namespace SyncNetPro.Contracts;

/// <summary>Kode <c>tran_type</c> SyncNet (nilai wire, jangan diubah).</summary>
public static class TranType
{
    /// <summary>Nilai wire <c>"STM"</c>.</summary>
    public const string MiniStatement = "STM";

    /// <summary>Nilai wire <c>"BAL"</c>.</summary>
    public const string BalanceInquiry = "BAL";

    /// <summary>Nilai wire <c>"WDL"</c>.</summary>
    public const string Withdrawal = "WDL";

    /// <summary>Nilai wire <c>"DEP"</c>.</summary>
    public const string Deposit = "DEP";

    /// <summary>Nilai wire <c>"BOK"</c>.</summary>
    public const string Booking = "BOK";

    /// <summary>Nilai wire <c>"INQ"</c>.</summary>
    public const string Inquiry = "INQ";

    /// <summary>Nilai wire <c>"PAY"</c>.</summary>
    public const string Payment = "PAY";

    /// <summary>Nilai wire <c>"PUR"</c>.</summary>
    public const string Purchase = "PUR";

    /// <summary>Nilai wire <c>"TRF"</c>.</summary>
    public const string Transfer = "TRF";

    /// <summary>Nilai wire <c>"TDB"</c>.</summary>
    public const string Debit = "TDB";

    /// <summary>Nilai wire <c>"TCR"</c>.</summary>
    public const string Credit = "TCR";

    /// <summary>Advice. Catatan: SDK lama juga memiliki <c>ADJUSTMENT</c> dengan nilai yang sama (<c>"ADV"</c>).</summary>
    public const string Advice = "ADV";

    /// <summary>Nilai wire <c>"VOD"</c>.</summary>
    public const string Void = "VOD";

    /// <summary>Nilai wire <c>"RFD"</c>.</summary>
    public const string Refund = "RFD";

    /// <summary>Nilai wire <c>"REV"</c>.</summary>
    public const string Reversal = "REV";

    /// <summary>Nilai wire <c>"ADM"</c>.</summary>
    public const string Admin = "ADM";

    /// <summary>Nilai wire <c>"SET"</c>.</summary>
    public const string Settlement = "SET";

    /// <summary>Nilai wire <c>"PIN"</c>.</summary>
    public const string PinChange = "PIN";

    /// <summary>Nilai wire <c>"KEY"</c>.</summary>
    public const string KeyChange = "KEY";

    /// <summary>Nilai wire <c>"CUT"</c>.</summary>
    public const string Cutover = "CUT";

    /// <summary>Virtual account: top up (credit).</summary>
    public const string VirtualTopUp = "VCR";

    /// <summary>Virtual account: adjust (debit).</summary>
    public const string VirtualAdjust = "VDB";

    /// <summary>Virtual account: saldo.</summary>
    public const string VirtualBalance = "VBA";
}
