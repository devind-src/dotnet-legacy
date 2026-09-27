using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.CardBins
{
    public class CreateCardBinRequest
    {
        [Required, MaxLength(16)]
        public string BinNr { get; set; } = string.Empty;

        [Required]
        public int GroupId { get; set; }

        [Required, MaxLength(20)]
        public string BinDesc { get; set; } = string.Empty;
    }
}
