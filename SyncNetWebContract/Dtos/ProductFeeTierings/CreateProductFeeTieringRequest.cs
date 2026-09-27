using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.ProductFeeTierings
{
    /// <summary>TierLevel 1-8 (legacy select has exactly 8 options). The combination of
    /// ProductId+TierLevel must be unique, and the [MinTran, MaxTran] range must not overlap
    /// any other tier of the same ProductId (legacy ProductFeesTieringIsDuplicateTierAsync /
    /// IsOverlapAsync).</summary>
    public class CreateProductFeeTieringRequest
    {
        [Required, MaxLength(30)]
        public string ProductId { get; set; } = string.Empty;

        [Required, Range(1, 8)]
        public int TierLevel { get; set; } = 1;

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
