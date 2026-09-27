using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.ProductFees
{
    /// <summary>FeeType: "0" Fixed / "1" Percent. For Fixed, FixedFeeAcq+FixedFeeIss+FixedFeeSwt
    /// must equal FixedFee exactly. For Percent, PercentFeeAcq+PercentFeeIss+PercentFeeSwt must
    /// total exactly 100. MerchantId/SubmerchantId/GroupName/SubgroupName are all optional —
    /// legacy only validates MerchantId's existence when non-empty (SubmerchantId is never
    /// existence-checked even in legacy).</summary>
    public class CreateProductFeeRequest
    {
        [Required, MaxLength(30)]
        public string ProductId { get; set; } = string.Empty;

        [MaxLength(15)]
        public string? MerchantId { get; set; }

        [MaxLength(30)]
        public string? SubmerchantId { get; set; }

        [MaxLength(50)]
        public string? GroupName { get; set; }

        [MaxLength(50)]
        public string? SubgroupName { get; set; }

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
    }
}
