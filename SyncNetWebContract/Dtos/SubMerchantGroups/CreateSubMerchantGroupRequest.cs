using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.SubMerchantGroups
{
    public class CreateSubMerchantGroupRequest
    {
        [Required, MaxLength(50)]
        public string GroupName { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string ParticipantId { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string ParentGroupName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Notes { get; set; }
    }
}
