namespace SyncNetApi.Dtos.CardOverrideLimits
{
    /// <summary>Same 31 nr_*/amt_* field shape as CardProductLimitDto (Card &gt; Profile &gt;
    /// Product &gt; Limit) — see that DTO's note on why it's a separate flat record rather than
    /// a shared base type.</summary>
    public record CardOverrideLimitDto(
        int Id,
        string? Issuer,
        string Pan,
        string? Channel,
        int? AmtPurchasePerTran,
        int? AmtCashPerTran,
        int? AmtPaymentPerTran,
        int? AmtTransferPerTran,
        int? NrInquiryDaily,
        int? NrPurchaseDaily,
        int? NrCashDaily,
        int? NrPaymentDaily,
        int? NrTransferDaily,
        int? AmtPurchaseDaily,
        int? AmtCashDaily,
        int? AmtPaymentDaily,
        int? AmtTransferDaily,
        int? NrInquiryWeekly,
        int? NrPurchaseWeekly,
        int? NrCashWeekly,
        int? NrPaymentWeekly,
        int? NrTransferWeekly,
        int? AmtPurchaseWeekly,
        int? AmtCashWeekly,
        int? AmtPaymentWeekly,
        int? AmtTransferWeekly,
        int? NrInquiryMonthly,
        int? NrPurchaseMonthly,
        int? NrCashMonthly,
        int? NrPaymentMonthly,
        int? NrTransferMonthly,
        int? AmtPurchaseMonthly,
        int? AmtCashMonthly,
        int? AmtPaymentMonthly,
        int? AmtTransferMonthly);
}
