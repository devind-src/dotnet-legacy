using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.ProductMerchants
{
    public class CreateProductMerchantRequest
    {
        [Required, MaxLength(15)]
        public string MerchantId { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string ProductId { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Notes { get; set; } = string.Empty;

        public bool Active { get; set; } = true;
    }
}
