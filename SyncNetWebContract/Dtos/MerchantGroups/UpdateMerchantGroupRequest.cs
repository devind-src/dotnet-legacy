using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.MerchantGroups
{
    /// <summary>ParticipantId is immutable after create (matches legacy UI, which renders it
    /// read-only on edit) — only Notes can change.</summary>
    public class UpdateMerchantGroupRequest
    {
        [MaxLength(50)]
        public string? Notes { get; set; }
    }
}
