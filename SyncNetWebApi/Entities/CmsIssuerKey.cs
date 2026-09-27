using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "cms_issuer_keys" — 1:1 child of cms_issuers (matched by `issuer`, no
    /// real FK constraint). PK `id`, GENERATED ALWAYS AS IDENTITY — never assign manually.
    /// Real table also has status/created_by/created_dt/updated_by/updated_dt columns, never
    /// mapped by the legacy model — dead columns, not mapped here (unlike cms_issuers/
    /// cms_issuer_contacts, this table has no last_update/update_by pair that legacy does
    /// write).</summary>
    [Table("cms_issuer_keys", Schema = "public")]
    public class CmsIssuerKey
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        [MaxLength(30)]
        public string issuer { get; set; } = string.Empty;
        [MaxLength(49)]
        public string? master_key { get; set; }
        [MaxLength(16)]
        public string? master_key_kcv { get; set; }
        [MaxLength(49)]
        public string? key_under_lmk { get; set; }
        [MaxLength(49)]
        public string? key_under_zmk { get; set; }
        [MaxLength(16)]
        public string? key_check_value { get; set; }
        [MaxLength(2)]
        public string? pinblock_format { get; set; }
        [MaxLength(1)]
        public string? key_length { get; set; }
    }
}
