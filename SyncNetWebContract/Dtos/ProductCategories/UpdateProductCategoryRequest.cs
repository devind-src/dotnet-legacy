using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.ProductCategories
{
    /// <summary>Category is immutable after create — legacy renders it readonly on edit.</summary>
    public class UpdateProductCategoryRequest
    {
        public bool IsTopup { get; set; }

        [Required, MaxLength(50)]
        public string Notes { get; set; } = string.Empty;
    }
}
