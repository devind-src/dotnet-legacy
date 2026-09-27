using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_routes_schedule" — menu Routing &gt; Jadwal Routing (`/route-schedule`).
    /// Satu baris = satu aturan dengan satu jendela waktu (Fase 3, DOCS/Routing/03-routing-dinamis/
    /// FASE-3-PRIORITY-TIME.md). rule_type CLOSED (Tutup) / OPEN (Jam Operasional) / PRIORITY
    /// (Prioritas Jadwal); recurrence ONCE / DAILY / WEEKLY / MONTHLY. Waktu lokal server.</summary>
    [Table("sw_routes_schedule", Schema = "public")]
    public class SwRoutesSchedule
    {
        [Key]
        public int id { get; set; }
        [MaxLength(50)]
        public string rule_name { get; set; } = string.Empty;
        [MaxLength(10)]
        public string rule_type { get; set; } = "CLOSED";
        [MaxLength(20)]
        public string? routing_type { get; set; }
        [MaxLength(11)]
        public string? inst_id { get; set; }
        public int? node_id { get; set; }
        public short? priority { get; set; }
        [MaxLength(10)]
        public string recurrence { get; set; } = "ONCE";
        [Column(TypeName = "timestamp(0) without time zone")]
        public DateTime? start_dt { get; set; }
        [Column(TypeName = "timestamp(0) without time zone")]
        public DateTime? end_dt { get; set; }
        [Column(TypeName = "time(0) without time zone")]
        public TimeSpan? time_start { get; set; }
        [Column(TypeName = "time(0) without time zone")]
        public TimeSpan? time_end { get; set; }
        [MaxLength(13)]
        public string? days_of_week { get; set; }
        [MaxLength(90)]
        public string? days_of_month { get; set; }
        public DateOnly? valid_from { get; set; }
        public DateOnly? valid_until { get; set; }
        [MaxLength(100)]
        public string? notes { get; set; }
        public string status { get; set; } = "1";
        [MaxLength(100)]
        public string? cancel_reason { get; set; }
        public string? cancelled_by { get; set; }
        [Column(TypeName = "timestamp without time zone")]
        public DateTime? cancelled_dt { get; set; }
        public string? created_by { get; set; }
        [Column(TypeName = "timestamp without time zone")]
        public DateTime? created_dt { get; set; }
        public string? updated_by { get; set; }
        [Column(TypeName = "timestamp without time zone")]
        public DateTime? updated_dt { get; set; }
    }
}
