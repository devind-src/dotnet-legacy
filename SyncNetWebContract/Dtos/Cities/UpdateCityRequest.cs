using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Cities
{
    public class UpdateCityRequest
    {
        [Required, MaxLength(50)]
        public string City { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Province { get; set; }

        [MaxLength(1)]
        public string? CityType { get; set; }
    }
}
