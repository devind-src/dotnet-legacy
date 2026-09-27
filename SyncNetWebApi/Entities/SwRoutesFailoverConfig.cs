using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_routes_failover_config" — menu Routing &gt; Health Check Config
    /// (`/route-failover-config`). Threshold/rc-code configuration for the failover engine.
    /// `inst_id` = null means the global default for `routing_type`; a row with `inst_id`
    /// filled in overrides the default for that product. Generic across routing
    /// strategies — see `routing_type`.</summary>
    [Table("sw_routes_failover_config", Schema = "public")]
    public class SwRoutesFailoverConfig
    {
        [Key]
        public int id { get; set; }
        public string routing_type { get; set; } = "MARGIN";
        [MaxLength(11)]
        public string? inst_id { get; set; }
        public string rc_link_down { get; set; } = "91,1091";
        public string rc_suspect { get; set; } = "68,1068";
        public int max_consecutive_suspect { get; set; } = 3;
        public int? link_down_cooldown_minutes { get; set; }
        public int? suspect_cooldown_minutes { get; set; }
        // Fase 2: daftar/ambang null = kategori nonaktif. rc_suspect dkk. = kategori Timeout.
        [MaxLength(50)]
        public string? rc_failed { get; set; }
        public int max_consecutive_failed { get; set; } = 3;
        public int? failed_cooldown_minutes { get; set; }
        [MaxLength(50)]
        public string? rc_pending { get; set; }
        public int max_consecutive_pending { get; set; } = 5;
        public int? pending_cooldown_minutes { get; set; }
        public int? latency_threshold_ms { get; set; }
        public int max_consecutive_latency { get; set; } = 3;
        public int? latency_cooldown_minutes { get; set; }
        public string is_active { get; set; } = "1";
        public string? updated_by { get; set; }
        [Column(TypeName = "timestamp without time zone")]
        public DateTime? updated_dt { get; set; }
    }
}
