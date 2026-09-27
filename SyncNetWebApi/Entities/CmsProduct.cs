using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "cms_products" — Card &gt; Profile &gt; Product (master row, distinct from
    /// Configuration &gt; Product's SwProduct/sw_product — different table, different concept:
    /// this is a card product definition per Issuer, that one is a topup/payment product).
    /// PK `id`, GENERATED ALWAYS AS IDENTITY — never assign manually. `issuer` is a REAL FK
    /// (ON DELETE NO ACTION) to cms_issuers.issuer — see CardIssuerService's delete guard.
    /// `profile_name`/`last_seq_nr` are shown in the legacy grid but never written by any form
    /// (read-only/derived, presumably an external process) — exposed as read-only in the DTO,
    /// never set by Create/Update. Real table also has status/created_by/created_dt/updated_by/
    /// updated_dt columns, never mapped by the legacy model — dead columns, not mapped here.</summary>
    [Table("cms_products", Schema = "public")]
    public class CmsProduct
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        [MaxLength(30)]
        public string? issuer { get; set; }
        [MaxLength(30)]
        public string? product { get; set; }
        [MaxLength(12)]
        public string? pan_prefix { get; set; }
        public short? pan_length { get; set; }
        [MaxLength(30)]
        public string? card_type { get; set; }
        [MaxLength(30)]
        public string? profile_name { get; set; }
        public int? last_seq_nr { get; set; }
    }
}
