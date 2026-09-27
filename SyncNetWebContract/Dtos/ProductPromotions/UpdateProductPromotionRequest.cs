using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.ProductPromotions
{
    /// <summary>ProductId/PromoCode/PromoDesc/DateStart/DateEnd are immutable after create —
    /// legacy renders all of them readonly/disabled on edit. Toggling IsActive back on can
    /// still trigger the overlap guard against other active promos.</summary>
    public class UpdateProductPromotionRequest
    {
        [Required, RegularExpression("^[01]$")]
        public string FeeType { get; set; } = "0";

        public int? FixedFee { get; set; }
        public int? FixedFeeAcq { get; set; }
        public int? FixedFeeIss { get; set; }
        public int? FixedFeeSwt { get; set; }
        public decimal? PercentFee { get; set; }
        public decimal? PercentFeeAcq { get; set; }
        public decimal? PercentFeeIss { get; set; }
        public decimal? PercentFeeSwt { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
