using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_merchant_key" — PosBase &gt; Merchant &gt; "Key Management" tab. Same
    /// HSM-placeholder caveat as SwTerminalKey — see that entity's note and
    /// KeyManagementController.</summary>
    [Table("sw_merchant_key", Schema = "public")]
    public class SwMerchantKey
    {
        [Key]
        public string merchant_id { get; set; } = string.Empty;
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
