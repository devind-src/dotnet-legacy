using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.ProductSupplierPrices
{
    /// <summary>SupplierId/BillerCode are immutable after create — legacy renders both
    /// readonly on edit.</summary>
    public class UpdateProductSupplierPriceRequest
    {
        public int? Denom { get; set; }

        [Required]
        public int HargaBeli { get; set; }

        public int? HargaJual { get; set; }

        public bool Active { get; set; } = true;
    }
}
