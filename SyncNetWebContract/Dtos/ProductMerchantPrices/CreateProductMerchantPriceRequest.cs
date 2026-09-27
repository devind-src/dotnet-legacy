using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.ProductMerchantPrices
{
    public class CreateProductMerchantPriceRequest
    {
        [Required, MaxLength(15)]
        public string MerchantId { get; set; } = string.Empty;

        [Required, MaxLength(15)]
        public string BillerCode { get; set; } = string.Empty;

        public int? Denom { get; set; }

        public int? HargaJual { get; set; }
    }
}
