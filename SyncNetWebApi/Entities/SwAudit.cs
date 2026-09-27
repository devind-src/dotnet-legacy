using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    [Table("sw_audit", Schema = "public")]
    public class SwAudit
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long id { get; set; }
        public string? type { get; set; } // I / U / D
        public string? tablename { get; set; }

        [Column(TypeName = "json")]
        public string? oldvalue { get; set; }

        [Column(TypeName = "json")]
        public string? newvalue { get; set; }

        // Real column is "timestamp without time zone" (verified live) — annotated explicitly
        // since Queries > Audit Trail (§7.27) now uses this column in a query-string-bound date
        // range filter; see SwTransPg.time_req / VA Statement (§7.21) for why an unannotated
        // DateTime here isn't just cosmetic (Npgsql defaults to "with time zone" otherwise, and
        // the service layer additionally strips Kind to Unspecified before filtering).
        [Column(TypeName = "timestamp without time zone")]
        public DateTime? updatedate { get; set; }
        public string? username { get; set; }
    }
}
