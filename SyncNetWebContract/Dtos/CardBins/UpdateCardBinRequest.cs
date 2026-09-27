using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.CardBins
{
    /// <summary>BinNr (PK) and GroupId are both immutable after create (disabled in the legacy
    /// edit form) — only BinDesc can be changed.</summary>
    public class UpdateCardBinRequest
    {
        [Required, MaxLength(20)]
        public string BinDesc { get; set; } = string.Empty;
    }
}
