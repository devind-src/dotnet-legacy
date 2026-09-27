namespace SyncNetApi.Dtos.CardProductLimits
{
    /// <summary>Same 31 nr_*/amt_* field shape as CardOverrideLimitDto (Card &gt; Profile &gt;
    /// Override Limit) — kept as a separate record rather than a shared base type since every
    /// other DTO in this codebase is a flat record with no inheritance; the 4-tab Razor form
    /// markup is shared client-side instead (Shared/CardLimitTabs.razor).</summary>
    public record CardProductLimitDto(
        int Id,
        int ProductId,
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
