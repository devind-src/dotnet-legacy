using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.ProductFees
{
    /// <summary>ProductId/MerchantId/SubmerchantId/GroupName/SubgroupName are immutable after
    /// create — legacy renders all of them readonly/disabled on edit.</summary>
    public class UpdateProductFeeRequest
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
    }
}
