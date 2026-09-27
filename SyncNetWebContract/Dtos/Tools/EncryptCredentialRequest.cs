using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Tools
{
    /// <summary>ConfigType: "JSON" (AES-256-CBC under Resources.bin's AES.Config key/IV) |
    /// "XML" (chunked DES under Resources.bin's DES.Config key) — matches legacy
    /// EncryptCreden.Index.razor's radio group exactly.</summary>
    public class EncryptCredentialRequest
    {
        [Required]
        public string Value { get; set; } = string.Empty;

        [Required]
        public string ConfigType { get; set; } = "JSON";
    }
}
