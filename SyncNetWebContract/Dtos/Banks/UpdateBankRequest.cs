using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Banks
{
    public class UpdateBankRequest
    {
        [Required, MaxLength(50)]
        public string Bank { get; set; } = string.Empty;

        [MaxLength(6)]
        public string? Cbc { get; set; }

        public bool Active { get; set; } = true;
    }
}
