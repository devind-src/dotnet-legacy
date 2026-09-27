using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "cms_branch" — Card &gt; Profile &gt; Issuer &gt; Branch (1:N child of
    /// cms_issuers). PK `id`, GENERATED ALWAYS AS IDENTITY — never assign manually. `issuer` is
    /// a REAL FK (ON DELETE NO ACTION) to cms_issuers.issuer — deleting an Issuer that still has
    /// Branch rows must be guarded (409), not attempted, or Postgres raises a raw FK-violation.
    /// Real table also has status/created_by/created_dt/updated_by/updated_dt columns, never
    /// mapped by the legacy model — dead columns, not mapped here. Column widths are wider than
    /// the legacy HTML form's maxlength attributes in several places (branch/city/phone/fax/
    /// email/contact are all varchar(50) here, address is varchar(255)) — real widths used,
    /// legacy's narrower maxlength not replicated (same precedent as Card &gt; Base &gt; BIN).</summary>
    [Table("cms_branch", Schema = "public")]
    public class CmsBranch
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        [MaxLength(30)]
        public string? issuer { get; set; }
        [MaxLength(3)]
        public string? idbranch { get; set; }
        [MaxLength(50)]
        public string? branch { get; set; }
        [MaxLength(255)]
        public string? address { get; set; }
        [MaxLength(50)]
        public string? city { get; set; }
        [MaxLength(50)]
        public string? phone { get; set; }
        [MaxLength(50)]
        public string? fax { get; set; }
        [MaxLength(50)]
        public string? email { get; set; }
        [MaxLength(50)]
        public string? contact { get; set; }
    }
}
