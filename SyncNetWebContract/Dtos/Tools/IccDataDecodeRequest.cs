using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Tools
{
    /// <summary>Value must be an even-length hex string (BER-TLV encoded ICC/EMV data) —
    /// validated in the service layer (matches legacy IccDataDecode.Validate()).</summary>
    public class IccDataDecodeRequest
    {
        [Required]
        public string Value { get; set; } = string.Empty;
    }
}
