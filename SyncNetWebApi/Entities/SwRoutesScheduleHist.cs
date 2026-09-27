using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_routes_schedule_hist" — nilai aturan Jadwal Routing SEBELUM diubah,
    /// dibatalkan, atau dihapus (action UPDATE / CANCEL / DELETE). Insert-only, tanpa FK ke
    /// sw_routes_schedule supaya riwayat aturan yang dihapus tetap ada.</summary>
    [Table("sw_routes_schedule_hist", Schema = "public")]
    public class SwRoutesScheduleHist
    {
        [Key]
        public long hist_id { get; set; }
        public int schedule_id { get; set; }
        [MaxLength(10)]
        public string action { get; set; } = "UPDATE";
        public string? rule_name { get; set; }
        public string? rule_type { get; set; }
        public string? routing_type { get; set; }
        public string? inst_id { get; set; }
        public int? node_id { get; set; }
        public short? priority { get; set; }
        public string? recurrence { get; set; }
        [Column(TypeName = "timestamp(0) without time zone")]
        public DateTime? start_dt { get; set; }
        [Column(TypeName = "timestamp(0) without time zone")]
        public DateTime? end_dt { get; set; }
        [Column(TypeName = "time(0) without time zone")]
        public TimeSpan? time_start { get; set; }
        [Column(TypeName = "time(0) without time zone")]
        public TimeSpan? time_end { get; set; }
        public string? days_of_week { get; set; }
        public string? days_of_month { get; set; }
        public DateOnly? valid_from { get; set; }
        public DateOnly? valid_until { get; set; }
        public string? notes { get; set; }
        public string? status { get; set; }
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
        public string changed_by { get; set; } = string.Empty;
        [Column(TypeName = "timestamp without time zone")]
        public DateTime changed_dt { get; set; }
    }
}
