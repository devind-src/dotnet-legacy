using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.SoundBoxes
{
    public class CreateSoundBoxRequest
    {
        [Required, MaxLength(30)]
        public string Nmid { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string StoreName { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Address { get; set; }

        [MaxLength(30)]
        public string? City { get; set; }

        [Required, MaxLength(50)]
        public string SerialNumber { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Provider { get; set; } = "newpos";

        [MaxLength(50)]
        public string? GroupName { get; set; }

        [MaxLength(50)]
        public string? SubgroupName { get; set; }
    }
}
