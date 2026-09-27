using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "cms_issuers" — Card &gt; Profile &gt; Issuer (master row). PK `issuer`
    /// varchar(30), manual (no identity). Real table also has status/created_by/created_dt
    /// columns, never mapped or written by the legacy model/form — dead columns, not mapped
    /// here (same pattern as Card &gt; Base). `last_update`/`update_by` ARE written by legacy
    /// (server-controlled, not user input).</summary>
    [Table("cms_issuers", Schema = "public")]
    public class CmsIssuer
    {
        [Key]
        [MaxLength(30)]
        public string issuer { get; set; } = string.Empty;
        [MaxLength(6)]
        public string? inst_id { get; set; }
        [MaxLength(3)]
        public string? currency { get; set; }
        [MaxLength(1)]
        public string? auth_service { get; set; }
        [MaxLength(1)]
        public string? velocity_per_tran { get; set; }
        [MaxLength(1)]
        public string? velocity_daily { get; set; }
        [MaxLength(1)]
        public string? velocity_weekly { get; set; }
        [MaxLength(1)]
        public string? velocity_monthly { get; set; }
        public int? max_pin_tries { get; set; }
        public DateTime? last_update { get; set; }
        [MaxLength(30)]
        public string? update_by { get; set; }
    }
}
