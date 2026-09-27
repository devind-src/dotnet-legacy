using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_crypto_keys" — Node encryption key material, 1:1 with sw_nodes
    /// (created/updated/deleted together — see NodeService). id was manually computed as
    /// max(id)+1 (legacy quirk); converted to a real Postgres IDENTITY column — DB now assigns
    /// it on insert. **10 real production rows already exist.** No real HSM
    /// integration exists in this system (see KeyManagementController) — the Generate buttons
    /// on the Node "Key Management" tab call the same placeholder random-hex endpoints already
    /// used by Terminal/Merchant Key Management (Phase 4), NOT real cryptography.
    /// `key_type` is NOT exposed in the legacy form at all — read-only, defaulted at create.</summary>
    [Table("sw_crypto_keys", Schema = "public")]
    public class SwCryptoKey
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        public int node_id { get; set; }
        public string? master_key { get; set; }
        public string? master_kcv { get; set; }
        public string? key_under_lmk { get; set; }
        public string? key_under_zmk { get; set; }
        public string? key_check_value { get; set; }
        public string? pinblock_format { get; set; }
        public string? key_length { get; set; }
        public string? key_type { get; set; }
        public string? status { get; set; }
        public string? created_by { get; set; }
        public DateTime? created_dt { get; set; }
        public string? updated_by { get; set; }
        public DateTime? updated_dt { get; set; }
    }
}
