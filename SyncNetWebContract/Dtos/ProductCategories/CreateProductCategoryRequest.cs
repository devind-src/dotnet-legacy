using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.ProductCategories
{
    public class CreateProductCategoryRequest
    {
        [Required, MaxLength(30)]
        public string Category { get; set; } = string.Empty;

        public bool IsTopup { get; set; }

        [Required, MaxLength(50)]
        public string Notes { get; set; } = string.Empty;
    }
}
