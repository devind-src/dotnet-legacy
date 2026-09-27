using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.ProductBins
{
    /// <summary>Id is NOT supplied by the caller — computed server-side as max(id)+1
    /// (sw_product_bins.id is not identity). bin is varchar(16), cbc is varchar(6), bank_name
    /// is varchar(50) — real column widths. This is Configuration &gt; Base &gt; BIN
    /// (sw_product_bins), NOT PosBase's Posnet BIN (sw_posnet_bin, Phase 4).</summary>
    public class CreateProductBinRequest
    {
        [Required, MaxLength(16)]
        public string Bin { get; set; } = string.Empty;

        [MaxLength(6)]
        public string? Cbc { get; set; }

        [MaxLength(50)]
        public string? BankName { get; set; }
    }
}
