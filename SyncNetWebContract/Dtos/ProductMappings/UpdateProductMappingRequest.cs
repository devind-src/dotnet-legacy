using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.ProductMappings
{
    /// <summary>AppName is immutable after create — legacy renders the select disabled on
    /// edit.</summary>
    public class UpdateProductMappingRequest
    {
        [Required, MaxLength(15)]
        public string SourceBillerCode { get; set; } = string.Empty;

        public int? Denom { get; set; }

        [Required, MaxLength(30)]
        public string DestBillerCode { get; set; } = string.Empty;

        public bool IsDeposit { get; set; }

        [Required, MaxLength(50)]
        public string Notes { get; set; } = string.Empty;
    }
}
