using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.CardAccounts
{
    /// <summary>GroupId is immutable after create (disabled in the legacy edit form) — only
    /// AccType and the verification flags can be changed.</summary>
    public class UpdateCardAccountRequest
    {
        [Required, MaxLength(2)]
        public string AccType { get; set; } = string.Empty;

        public bool PanVerification { get; set; }

        public bool PinVerification { get; set; }
    }
}
