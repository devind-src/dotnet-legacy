using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.CardHotcards
{
    /// <summary>CardNr is capped at the real column width (varchar(19)), but the legacy
    /// business rule additionally requires it to be exactly 16 characters — enforced here via
    /// StringLength(16, MinimumLength = 16), replicating legacy's "Invalid card number" check.</summary>
    public class CreateCardHotcardRequest
    {
        [Required, StringLength(16, MinimumLength = 16, ErrorMessage = "Card number must be exactly 16 characters.")]
        public string CardNr { get; set; } = string.Empty;

        [Required, MaxLength(2)]
        public string RespCode { get; set; } = string.Empty;

        [Required, MaxLength(6)]
        public string AuthIdResp { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Notes { get; set; }
    }
}
