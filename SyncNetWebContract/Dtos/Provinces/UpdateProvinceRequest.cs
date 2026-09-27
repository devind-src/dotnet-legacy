using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Provinces
{
    public class UpdateProvinceRequest
    {
        [Required, MaxLength(50)]
        public string Province { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Capital { get; set; }

        [MaxLength(50)]
        public string? Island { get; set; }
    }
}
