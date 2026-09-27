using System;
using System.Collections.Generic;

namespace SyncNet.Models
{
    // satu aturan Jadwal Routing (sw_routes_schedule, Fase 3). hanya aturan aktif yang dimuat.
    // NodeName = sw_nodes.node_name (sama dengan supplier_id health check).
    public class RoutingScheduleModel
    {
        public const string CLOSED = "CLOSED";      // Tutup
        public const string OPEN = "OPEN";          // Jam Operasional
        public const string PRIORITY = "PRIORITY";  // Prioritas Jadwal

        public const string ONCE = "ONCE";
        public const string DAILY = "DAILY";
        public const string WEEKLY = "WEEKLY";
        public const string MONTHLY = "MONTHLY";

        public int Id { get; set; }
        public string Name { get; set; }
        public string RuleType { get; set; }
        public string RoutingType { get; set; }     // MARGIN / PRODUCT / null = keduanya
        public string InstId { get; set; }          // null = semua produk biller
        public string NodeName { get; set; }
        public short? Priority { get; set; }
        public string Recurrence { get; set; }
        public DateTime? StartDt { get; set; }
        public DateTime? EndDt { get; set; }
        public TimeSpan? TimeStart { get; set; }
        public TimeSpan? TimeEnd { get; set; }
        public List<int> Days { get; set; } = [];    // WEEKLY: 1=Senin..7=Minggu, MONTHLY: tanggal
        public DateTime? ValidFrom { get; set; }    // tanggal saja
        public DateTime? ValidUntil { get; set; }

        public bool IsOnce => Recurrence == ONCE;
    }
}
