using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_routes_supplier_status" — menu Routing &gt; Supplier Status
    /// (`/route-supplier-status`). Runtime health state per supplier, written by the
    /// switching engine (rc91/rc68 handling) and read by MarginRoutingStrategy to skip
    /// DOWN/SUSPECT suppliers. Generic across routing strategies — tracked per
    /// supplier_id only, not per product. Dashboard only reads it and offers a manual
    /// "Reset ke Active" action; there is no create/delete from the UI.</summary>
    [Table("sw_routes_supplier_status", Schema = "public")]
    public class SwRoutesSupplierStatus
    {
        [Key]
        [MaxLength(20)]
        public string supplier_id { get; set; } = string.Empty;
        public string status { get; set; } = "ACTIVE";
        public string? last_rc_code { get; set; }
        public int consecutive_suspect_count { get; set; }
        public int retry_count { get; set; }
        // Fase 2. consecutive_suspect_count = counter Timeout.
        public int consecutive_failed_count { get; set; }
        public int consecutive_pending_count { get; set; }
        public int consecutive_latency_count { get; set; }
        [MaxLength(20)]
        public string? block_reason { get; set; }
        public int? last_latency_ms { get; set; }
        [Column(TypeName = "timestamp without time zone")]
        public DateTime? last_tran_dt { get; set; }
        [Column(TypeName = "timestamp without time zone")]
        public DateTime? blocked_since { get; set; }
        [Column(TypeName = "timestamp without time zone")]
        public DateTime? blocked_until { get; set; }
        public string? updated_by { get; set; }
        [Column(TypeName = "timestamp without time zone")]
        public DateTime? updated_dt { get; set; }
    }
}
