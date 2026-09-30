using System;

namespace SyncNet.Models
{
    // satu aturan Volume & Tiering (sw_routes_commitment, Fase 5). hanya aturan aktif yang masa
    // berlakunya belum lewat yang dimuat. NodeName = sw_nodes.node_name (= supplier_id health check).
    public class CommitmentRuleModel
    {
        public const string LIMIT = "LIMIT";     // Kuota: biller dibuang bila volume >= nilai
        public const string TARGET = "TARGET";   // Target Tier: biller didahulukan selama volume < nilai

        public const string COUNT = "COUNT";
        public const string AMOUNT = "AMOUNT";

        public const string DAILY = "DAILY";
        public const string MONTHLY = "MONTHLY";

        public int Id { get; set; }
        public string Name { get; set; }
        public string RuleType { get; set; }
        public string RoutingType { get; set; }     // MARGIN / PRODUCT
        public string NodeName { get; set; }
        public string InstId { get; set; }          // null = semua produk biller pada kategori ini
        public string Metric { get; set; }
        public string PeriodType { get; set; }
        public decimal Threshold { get; set; }
        public short? WarnPct { get; set; }
        public DateTime? ValidFrom { get; set; }    // tanggal saja
        public DateTime? ValidUntil { get; set; }
        public DateTime? CreatedDt { get; set; }

        public bool IsValidOn(DateTime day) =>
            (ValidFrom.HasValue == false || ValidFrom.Value.Date <= day.Date) &&
            (ValidUntil.HasValue == false || ValidUntil.Value.Date >= day.Date);

        public bool InScope(string routingType, string nodeName, string productId) =>
            RoutingType == routingType && NodeName == nodeName && (InstId == null || InstId == productId);

        // awal periode yang memuat `day` (hari itu / tanggal 1 bulan itu)
        public DateTime PeriodStart(DateTime day) =>
            PeriodType == MONTHLY ? new DateTime(day.Year, day.Month, 1) : day.Date;
    }

    // volume harian satu biller + produk (sw_routes_volume). dipakai juga sebagai delta yang
    // ditambahkan ke tabel saat flush.
    public class VolumeModel
    {
        public DateTime Date { get; set; }
        public string RoutingType { get; set; }
        public string NodeName { get; set; }
        public string InstId { get; set; }
        public long Count { get; set; }
        public decimal Amount { get; set; }
    }

    // total volume satu biller + produk yang dimuat saat flush (agregat di DB, bukan baris harian):
    // hari ini, kemarin, bulan ini s.d. hari ini, dan bulan lalu, relatif terhadap tanggal muat.
    public class VolumeTotalModel
    {
        public string RoutingType { get; set; }
        public string NodeName { get; set; }
        public string InstId { get; set; }
        public long TodayCount { get; set; }
        public decimal TodayAmount { get; set; }
        public long YesterdayCount { get; set; }
        public decimal YesterdayAmount { get; set; }
        public long MonthCount { get; set; }
        public decimal MonthAmount { get; set; }
        public long PrevMonthCount { get; set; }
        public decimal PrevMonthAmount { get; set; }
    }

    // event sw_routes_commitment_log (satu per aturan + periode + event + nilai)
    public class CommitmentLogModel
    {
        public const string LIMIT_WARN = "LIMIT_WARN";
        public const string LIMIT_REACHED = "LIMIT_REACHED";
        public const string LIMIT_IGNORED = "LIMIT_IGNORED";
        public const string TARGET_REACHED = "TARGET_REACHED";
        public const string TARGET_MISSED = "TARGET_MISSED";

        public int CommitmentId { get; set; }
        public string RuleType { get; set; }
        public string RoutingType { get; set; }
        public string NodeName { get; set; }
        public string InstId { get; set; }
        public string Metric { get; set; }
        public string PeriodType { get; set; }
        public DateTime PeriodStart { get; set; }
        public string Event { get; set; }
        public decimal VolumeValue { get; set; }
        public decimal ThresholdValue { get; set; }
        public DateTime CreatedDt { get; set; }
    }
}
