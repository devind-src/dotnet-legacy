using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.ProductMerchants
{
    /// <summary>MerchantId is immutable after create — legacy renders it readonly on
    /// edit.</summary>
    public class UpdateProductMerchantRequest
    {
        [Required, MaxLength(50)]
        public string ProductId { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Notes { get; set; } = string.Empty;

        public bool Active { get; set; } = true;
    }
}
