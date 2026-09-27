using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.ProductSupplierPrices
{
    /// <summary>SupplierId is free text copied from a Node-Biller dropdown — legacy does not
    /// validate it exists, only that it's non-empty. BillerCode (product code) must exist in
    /// Product Master. HargaBeli is mandatory (legacy: "Purchase price mandatory") and the
    /// computed Margin (HargaJual - HargaBeli) must not be negative.</summary>
    public class CreateProductSupplierPriceRequest
    {
        [Required, MaxLength(15)]
        public string SupplierId { get; set; } = string.Empty;

        [Required, MaxLength(15)]
        public string BillerCode { get; set; } = string.Empty;

        public int? Denom { get; set; }

        [Required]
        public int HargaBeli { get; set; }

        public int? HargaJual { get; set; }

        public bool Active { get; set; } = true;
    }
}
