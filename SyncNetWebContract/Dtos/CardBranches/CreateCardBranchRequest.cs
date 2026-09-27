using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.CardBranches
{
    /// <summary>Only IdBranch and Branch are actually validated as mandatory by legacy
    /// IsValid() — Address/City/Phone/Fax/Email/Contact all have `required` in the legacy HTML
    /// markup but are never actually checked in code, so they stay optional here too.</summary>
    public class CreateCardBranchRequest
    {
        [Required, MaxLength(30)]
        public string Issuer { get; set; } = string.Empty;

        [Required, MaxLength(3)]
        public string IdBranch { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Branch { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Address { get; set; }
        [MaxLength(50)]
        public string? City { get; set; }
        [MaxLength(50)]
        public string? Phone { get; set; }
        [MaxLength(50)]
        public string? Fax { get; set; }
        [MaxLength(50)]
        public string? Email { get; set; }
        [MaxLength(50)]
        public string? Contact { get; set; }
    }
}
