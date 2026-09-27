using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.SupportMembers
{
    /// <summary>MemberId is NOT supplied by the caller — computed server-side as max(id)+1
    /// (sw_support_members.member_id is not identity). TeamId must reference an existing team
    /// (real Postgres FK constraint). name/email are varchar(50), phone is varchar(30) — real
    /// column widths, all required to match legacy validation (SupportMemberDetail.IsValid).</summary>
    public class CreateSupportMemberRequest
    {
        [Required]
        public int TeamId { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string Phone { get; set; } = string.Empty;
    }
}
