using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_routes_failover_log" — menu Routing &gt; Failover Log
    /// (`/route-failover-log`). Insert-only audit trail written by the switching engine
    /// whenever a supplier transitions to DOWN/SUSPECT. Read-only from the dashboard.</summary>
    [Table("sw_routes_failover_log", Schema = "public")]
    public class SwRoutesFailoverLog
    {
        [Key]
        public int id { get; set; }
        public string routing_type { get; set; } = "MARGIN";
        public string? inst_id { get; set; }
        public int? denom { get; set; }
        public string? trace_number { get; set; }
        public string? from_supplier_id { get; set; }
        public string? to_supplier_id { get; set; }
        public string? rc_code { get; set; }
        public string? reason { get; set; }
        public int? latency_ms { get; set; }
        // Fase 3: aturan Jadwal Routing penyebab penolakan (reason SCHEDULE_CLOSED)
        public int? schedule_id { get; set; }
        [Column(TypeName = "timestamp without time zone")]
        public DateTime created_dt { get; set; }
    }
}
