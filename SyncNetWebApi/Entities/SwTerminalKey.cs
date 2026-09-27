using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_term_key" — PosBase &gt; Terminal &gt; "Key Management" tab. Legacy
    /// populates master_key/master_kcv/key_under_lmk/key_under_zmk/key_check_value via a real
    /// HSM call (NbHSM.GenerateMasterKey / GetKeyCheckValueUnderLMK / GenerateSessionKey —
    /// actual LMK/ZMK key-ceremony cryptography). No HSM exists in this API, so these fields
    /// are plain manual entry here — do NOT fabricate crypto-looking values client-side, that
    /// would produce keys with no real cryptographic relationship and could be mistaken for
    /// genuine terminal encryption material.</summary>
    [Table("sw_term_key", Schema = "public")]
    public class SwTerminalKey
    {
        [Key]
        public string term_id { get; set; } = string.Empty;
        public string? key_type { get; set; }
        public string? key_length { get; set; }
        public string? master_key { get; set; }
        public string? master_kcv { get; set; }
        public string? key_under_lmk { get; set; }
        public string? key_under_zmk { get; set; }
        public string? key_check_value { get; set; }
        public string? pinblock_format { get; set; }
        public string? status { get; set; }
    }
}
