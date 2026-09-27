using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    [Table("dashboard_user_log", Schema = "public")]
    public class DashboardUserLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long log_id { get; set; }

        public string? user_name { get; set; }

        // Real column is "timestamp without time zone" (verified live) — annotated explicitly
        // since Queries > User Log (§7.27) now uses this column in a query-string-bound date
        // range filter; see SwTransPg.time_req / VA Statement (§7.21) for why this matters.
        [Column(TypeName = "timestamp without time zone")]
        public DateTime? date_time { get; set; }
        public string? host_address { get; set; }
        public string? host_name { get; set; }
        public string? host_agent { get; set; }

        /// <summary>1 = login, 0 = logout</summary>
        public string? state { get; set; }
    }
}
