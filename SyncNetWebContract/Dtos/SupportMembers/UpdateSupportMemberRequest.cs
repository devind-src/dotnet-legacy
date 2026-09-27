using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.SupportMembers
{
    /// <summary>Unlike Team's Name, legacy leaves every Member field editable on update
    /// (SupportMemberDetail.razor has no readonly/disabled attributes) — including TeamId.</summary>
    public class UpdateSupportMemberRequest
    {
        [Required]
        public int TeamId { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string Phone { get; set; } = string.Empty;

        public bool Active { get; set; } = true;
    }
}
