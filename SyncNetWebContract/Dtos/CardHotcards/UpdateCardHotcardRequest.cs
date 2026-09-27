using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.CardHotcards
{
    /// <summary>CardNr (PK) is immutable after create — only RespCode/AuthIdResp/Notes can be
    /// changed.</summary>
    public class UpdateCardHotcardRequest
    {
        [Required, MaxLength(2)]
        public string RespCode { get; set; } = string.Empty;

        [Required, MaxLength(6)]
        public string AuthIdResp { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Notes { get; set; }
    }
}
