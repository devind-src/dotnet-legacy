using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.MerchantGroups
{
    public class CreateMerchantGroupRequest
    {
        [Required, MaxLength(50)]
        public string GroupName { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string ParticipantId { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Notes { get; set; }
    }
}
