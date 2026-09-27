using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.CardBranches
{
    /// <summary>Issuer is immutable after create (set from the parent Issuer, readonly in the
    /// legacy edit form).</summary>
    public class UpdateCardBranchRequest
    {
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
