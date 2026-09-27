using System;
using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.ProductPromotions
{
    /// <summary>FeeType: "0" Fixed / "1" Percent, same total-must-match rules as Product Fees.
    /// DateEnd must not be earlier than DateStart. The date range must not overlap any other
    /// *active* promo for the same ProductId (legacy ProductFeesPromoIsOverlapAsync — only
    /// active rows are checked).</summary>
    public class CreateProductPromotionRequest
    {
        [Required, MaxLength(30)]
        public string ProductId { get; set; } = string.Empty;

        [Required, MaxLength(15)]
        public string PromoCode { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? PromoDesc { get; set; }

        [Required]
        public DateTime DateStart { get; set; }

        [Required]
        public DateTime DateEnd { get; set; }

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
