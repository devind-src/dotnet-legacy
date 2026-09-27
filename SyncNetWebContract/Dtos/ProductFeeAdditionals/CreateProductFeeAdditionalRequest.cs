using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.ProductFeeAdditionals
{
    /// <summary>Fee must be non-zero and at most 100,000 (legacy: "Max additional fee 100
    /// ribu").</summary>
    public class CreateProductFeeAdditionalRequest
    {
        [Required, MaxLength(50)]
        public string GroupName { get; set; } = string.Empty;

        [Required, Range(1, 100_000)]
        public int Fee { get; set; }
    }
}
