using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Brands
{
    public class UpdateBrandRequest
    {
        [Required, MaxLength(50)]
        public string Brand { get; set; } = string.Empty;

        public bool Active { get; set; } = true;
    }
}
