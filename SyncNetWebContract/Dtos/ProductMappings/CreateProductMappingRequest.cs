using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.ProductMappings
{
    /// <summary>SourceBillerCode ("Product" in legacy UI) is varchar(15), DestBillerCode
    /// ("Supplier Biller Code") is varchar(30) — widths verified live. Legacy has a
    /// commented-out check that source_biller_code must exist in Product Master — intentionally
    /// disabled in legacy, not replicated here either.</summary>
    public class CreateProductMappingRequest
    {
        [Required, MaxLength(50)]
        public string AppName { get; set; } = string.Empty;

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
