using System;
using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.RouteSchedules
{
    /// <summary>Tambah / ubah aturan Jadwal Routing. Field yang dipakai bergantung pada RuleType dan
    /// Recurrence (validasi lengkap di service): ONCE = StartDt + EndDt; DAILY/WEEKLY/MONTHLY =
    /// TimeStart + TimeEnd (+ DaysOfWeek / DaysOfMonth), ValidFrom/ValidUntil opsional. NodeId wajib;
    /// InstId kosong = semua produk biller (semua jenis). PRIORITY wajib Priority.</summary>
    public class SaveRouteScheduleRequest
    {
        [Required, MaxLength(50)]
        public string RuleName { get; set; } = string.Empty;

        [Required, MaxLength(10)]
        public string RuleType { get; set; } = RouteScheduleCodes.Closed;

        [MaxLength(20)]
        public string? RoutingType { get; set; }

        [MaxLength(11)]
        public string? InstId { get; set; }

        public int? NodeId { get; set; }

        public short? Priority { get; set; }

        [Required, MaxLength(10)]
        public string Recurrence { get; set; } = RouteScheduleCodes.Once;

        public DateTime? StartDt { get; set; }
        public DateTime? EndDt { get; set; }
        public TimeSpan? TimeStart { get; set; }
        public TimeSpan? TimeEnd { get; set; }

        [MaxLength(13)]
        public string? DaysOfWeek { get; set; }

        [MaxLength(90)]
        public string? DaysOfMonth { get; set; }

        public DateOnly? ValidFrom { get; set; }
        public DateOnly? ValidUntil { get; set; }

        [MaxLength(100)]
        public string? Notes { get; set; }
    }

    public class CancelRouteScheduleRequest
    {
        [Required, MaxLength(100)]
        public string Reason { get; set; } = string.Empty;
    }
}
