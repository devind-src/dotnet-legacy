using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.ProductPostpaidFees
{
    /// <summary>FeeType: "0" Fixed / "1" Percent (Percent hanya untuk routing STATIC). STATIC
    /// Fixed: Acq+Mer+Iss+Bil+Swt harus sama dengan FixedFee; Percent: totalnya 100. Mode
    /// dynamic: Bil dan Swt dihitung dari sharing fee biller, Acq+Mer+Iss tidak boleh melebihi
    /// FixedFee maupun sharing fee setiap biller. MerchantId/SubmerchantId/GroupName/SubgroupName are all optional —
    /// legacy only validates MerchantId's existence when non-empty (SubmerchantId is never
    /// existence-checked even in legacy).</summary>
    public class CreateProductPostpaidFeeRequest
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
