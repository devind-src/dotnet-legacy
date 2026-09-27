using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.CardProductLimits
{
    /// <summary>ProductId is immutable after create (set from the parent Product, same pattern
    /// as Connection's NodeId) — not part of this request. Channel stays editable, matching
    /// legacy (no readonly noted for it in Detail.razor).</summary>
    public class UpdateCardProductLimitRequest
    {
        [Required, MaxLength(4)]
        public string Channel { get; set; } = string.Empty;

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
