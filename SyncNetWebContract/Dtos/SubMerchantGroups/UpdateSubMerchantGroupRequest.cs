using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.SubMerchantGroups
{
    /// <summary>ParticipantId is immutable after create (matches legacy UI, read-only on
    /// edit) — only ParentGroupName/Notes can change.</summary>
    public class UpdateSubMerchantGroupRequest
    {
        [Required, MaxLength(50)]
        public string ParentGroupName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Notes { get; set; }
    }
}
