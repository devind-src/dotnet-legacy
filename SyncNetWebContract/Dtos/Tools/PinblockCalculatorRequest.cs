using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Tools
{
    /// <summary>Format: "ISO ANSI" | "DOCUTEL" | "IBM" | "PLUS". Pan must be exactly 16 digits,
    /// Pin exactly 6 digits, Key hex 16/32/48 chars — all validated in the service layer
    /// (matches legacy PinBlock.Validate()).</summary>
    public class PinblockCalculatorRequest
    {
        [Required]
        public string Format { get; set; } = "ISO ANSI";

        [Required]
        public string Pan { get; set; } = string.Empty;

        [Required]
        public string Pin { get; set; } = string.Empty;

        [Required]
        public string Key { get; set; } = string.Empty;
    }
}
