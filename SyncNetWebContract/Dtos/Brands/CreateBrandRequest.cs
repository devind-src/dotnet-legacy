using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Brands
{
    /// <summary>Id is NOT supplied by the caller — computed server-side as max(id)+1 (sw_brand.id
    /// is not identity). brand is varchar(50) — real column width.</summary>
    public class CreateBrandRequest
    {
        [Required, MaxLength(50)]
        public string Brand { get; set; } = string.Empty;
    }
}
