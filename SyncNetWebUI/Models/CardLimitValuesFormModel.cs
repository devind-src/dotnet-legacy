namespace SyncNetWasm.Models
{
    /// <summary>Plain form-model holding the 31 nr_*/amt_* limit fields shared identically by
    /// Card &gt; Profile &gt; Product &gt; Limit and Card &gt; Profile &gt; Override Limit — bound by
    /// Shared/CardLimitTabs.razor so the 4-tab markup (Per Transaction/Daily/Weekly/Monthly)
    /// isn't duplicated between the two forms. Not tied to any single API DTO — each parent
    /// form maps these fields into its own Create/Update request.</summary>
    public class CardLimitValuesFormModel
    {
        public int? AmtPurchasePerTran { get; set; }
        public int? AmtCashPerTran { get; set; }
        public int? AmtPaymentPerTran { get; set; }
        public int? AmtTransferPerTran { get; set; }

        public int? NrInquiryDaily { get; set; }
        public int? NrPurchaseDaily { get; set; }
        public int? NrCashDaily { get; set; }
        public int? NrPaymentDaily { get; set; }
        public int? NrTransferDaily { get; set; }
        public int? AmtPurchaseDaily { get; set; }
        public int? AmtCashDaily { get; set; }
        public int? AmtPaymentDaily { get; set; }
        public int? AmtTransferDaily { get; set; }

        public int? NrInquiryWeekly { get; set; }
        public int? NrPurchaseWeekly { get; set; }
        public int? NrCashWeekly { get; set; }
        public int? NrPaymentWeekly { get; set; }
        public int? NrTransferWeekly { get; set; }
        public int? AmtPurchaseWeekly { get; set; }
        public int? AmtCashWeekly { get; set; }
        public int? AmtPaymentWeekly { get; set; }
        public int? AmtTransferWeekly { get; set; }

        public int? NrInquiryMonthly { get; set; }
        public int? NrPurchaseMonthly { get; set; }
        public int? NrCashMonthly { get; set; }
        public int? NrPaymentMonthly { get; set; }
        public int? NrTransferMonthly { get; set; }
        public int? AmtPurchaseMonthly { get; set; }
        public int? AmtCashMonthly { get; set; }
        public int? AmtPaymentMonthly { get; set; }
        public int? AmtTransferMonthly { get; set; }
    }
}
