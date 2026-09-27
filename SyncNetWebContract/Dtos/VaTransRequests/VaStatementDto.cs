using System;

namespace SyncNetApi.Dtos.VaTransRequests
{
    /// <summary>Row shape for Virtual Account &gt; Statement — mirrors legacy's VaStatement
    /// (a query DTO over sw_trans_pg joined to va_account, not a table of its own).
    /// TranType is either a real sw_trans_pg.tran_type ("VCR","VDB","WDL","DEP","TRF","PUR",
    /// "PAY") or the synthetic "REV" used for a reversal's duplicated display row.
    /// Amount is always positive (sw_trans_pg.amount_va) — whether it displays as debet or
    /// credit is a presentation decision based on TranType (VCR/REV = credit, everything else
    /// = debet), same split legacy's GetAmountDebet/GetAmountCredit render-time helpers make.</summary>
    public record VaStatementDto(
        long TranNr,
        string? VaName,
        string? VaAccount,
        string? GroupName,
        DateTime? TrxDate,
        string? TranType,
        string? Product,
        string? ToAccount,
        string? Refnum,
        decimal Amount,
        decimal Balance);
}
