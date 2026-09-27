using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.ProductFeeTierings
{
    /// <summary>ProductId/TierLevel are immutable after create — legacy renders both
    /// readonly/disabled on edit. MinTran/MaxTran stay editable (still re-checked for overlap
    /// against other tiers of the same product).</summary>
    public class UpdateProductFeeTieringRequest
    {
        public long? MinTran { get; set; }
        public long? MaxTran { get; set; }

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
