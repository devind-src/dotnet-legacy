using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.ProductPrepaidPricing
{
    /// <summary>SupplierId/BillerCode are immutable after create — legacy renders both
    /// readonly on edit.</summary>
    public class UpdateProductPrepaidPricingRequest
    {
        public int? Denom { get; set; }

        [Required]
        public int HargaBeli { get; set; }

        public int? HargaJual { get; set; }

        public bool Active { get; set; } = true;
        /// <summary>Priority supplier per produk + denom (unik). Kosong = diisi urutan berikutnya.</summary>
        [Range(1, 32000)]
        public int? Priority { get; set; }
        /// <summary>Bobot Load Balance (0-100).</summary>
        [Range(0, 100)]
        public int? LbWeight { get; set; }
    }
}
