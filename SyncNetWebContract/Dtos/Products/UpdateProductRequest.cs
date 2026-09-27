using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Products
{
    /// <summary>ProductCode is immutable after create (it's the PK).</summary>
    public class UpdateProductRequest
    {
        [Required, MaxLength(50)]
        public string ProductName { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? Category { get; set; }

        public bool Active { get; set; } = true;
    }
}
