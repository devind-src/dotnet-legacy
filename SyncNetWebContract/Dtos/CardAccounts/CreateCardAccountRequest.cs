using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.CardAccounts
{
    /// <summary>Id is NOT supplied by the caller — computed server-side as max(id)+1
    /// (sw_accounts.id is not identity).</summary>
    public class CreateCardAccountRequest
    {
        [Required]
        public int GroupId { get; set; }

        [Required, MaxLength(2)]
        public string AccType { get; set; } = string.Empty;

        public bool PanVerification { get; set; }

        public bool PinVerification { get; set; }
    }
}
