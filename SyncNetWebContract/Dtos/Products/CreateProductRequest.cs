using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Products
{
    /// <summary>product_code is varchar(15), product_name is varchar(50), category is
    /// varchar(30) — real column widths. Category is free text here (not a dropdown tied to
    /// sw_product_category — that CRUD is out of scope for this minimal Product Master).</summary>
    public class CreateProductRequest
    {
        [Required, MaxLength(15)]
        public string ProductCode { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string ProductName { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? Category { get; set; }
    }
}
