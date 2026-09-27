using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.ProductPostpaidFees
{
    /// <summary>ProductId/MerchantId/SubmerchantId/GroupName/SubgroupName are immutable after
    /// create — legacy renders all of them readonly/disabled on edit.</summary>
    public class UpdateProductPostpaidFeeRequest
    {
        [Required, RegularExpression("^[01]$")]
        public string FeeType { get; set; } = "0";

        public int? FixedFee { get; set; }
        public int? FixedFeeAcq { get; set; }
        public int? FixedFeeMer { get; set; }
        public int? FixedFeeIss { get; set; }
        public int? FixedFeeBil { get; set; }
        public int? FixedFeeSwt { get; set; }
        public decimal? PercentFee { get; set; }
        public decimal? PercentFeeAcq { get; set; }
        public decimal? PercentFeeMer { get; set; }
        public decimal? PercentFeeIss { get; set; }
        public decimal? PercentFeeBil { get; set; }
        public decimal? PercentFeeSwt { get; set; }
        /// <summary>STATIC/PRIORITY/BEST_PRICE/LOAD_BALANCE. Null hanya boleh di baris per CA
        /// (Ikuti Default); baris default produk yang null disimpan sebagai STATIC.</summary>
        [MaxLength(12)]
        public string? RoutingMode { get; set; }
        /// <summary>Biller untuk mode STATIC (node biller produk). Null = primary.</summary>
        public int? StaticNodeId { get; set; }
    }
}
