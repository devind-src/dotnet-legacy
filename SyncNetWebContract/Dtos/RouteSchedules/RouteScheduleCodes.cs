using System.Collections.Generic;
using System.Linq;

namespace SyncNetApi.Dtos.RouteSchedules
{
    /// <summary>Kode Jadwal Routing (Fase 3) dan labelnya, dipakai bersama backend dan dashboard.</summary>
    public static class RouteScheduleCodes
    {
        // rule_type
        public const string Closed = "CLOSED";
        public const string Open = "OPEN";
        public const string Priority = "PRIORITY";

        // recurrence
        public const string Once = "ONCE";
        public const string Daily = "DAILY";
        public const string Weekly = "WEEKLY";
        public const string Monthly = "MONTHLY";

        // routing_type (null = keduanya)
        public const string Margin = "MARGIN";
        public const string Product = "PRODUCT";

        // status tampilan (dihitung dari jam server, bukan kolom)
        public const string StateUpcoming = "UPCOMING";
        public const string StateRunning = "RUNNING";
        public const string StateActive = "ACTIVE";
        public const string StateFinished = "FINISHED";
        public const string StateCancelled = "CANCELLED";

        /// <summary>Alasan di sw_routes_failover_log saat transaksi ditolak karena jadwal.</summary>
        public const string LogReasonScheduleClosed = "SCHEDULE_CLOSED";

        public static readonly string[] RuleTypes = [Closed, Open, Priority];
        public static readonly string[] Recurrences = [Once, Daily, Weekly, Monthly];
        public static readonly string[] RoutingTypes = [Margin, Product];
        public static readonly string[] States = [StateUpcoming, StateRunning, StateActive, StateFinished, StateCancelled];

        public static string RuleTypeLabel(string? code) => code switch
        {
            Closed => "Tutup",
            Open => "Jam Operasional",
            Priority => "Prioritas Jadwal",
            _ => code ?? "-"
        };

        public static string RecurrenceLabel(string? code) => code switch
        {
            Once => "Sekali",
            Daily => "Harian",
            Weekly => "Mingguan",
            Monthly => "Bulanan",
            _ => code ?? "-"
        };

        public static string RoutingTypeLabel(string? code) => code switch
        {
            Margin => "Topup",
            Product => "Bill Payment",
            null or "" => "Semua",
            _ => code
        };

        public static string StateLabel(string? code) => code switch
        {
            StateUpcoming => "Akan datang",
            StateRunning => "Berlangsung",
            StateActive => "Aktif",
            StateFinished => "Selesai",
            StateCancelled => "Dibatalkan",
            _ => code ?? "-"
        };

        private static readonly string[] DayNames = ["Sen", "Sel", "Rab", "Kam", "Jum", "Sab", "Min"];

        /// <summary>Hari ISO 1 = Senin .. 7 = Minggu.</summary>
        public static string DayName(int isoDay) => isoDay is >= 1 and <= 7 ? DayNames[isoDay - 1] : isoDay.ToString();

        /// <summary>"1,2,3,4,5" -> [1,2,3,4,5]; nilai yang bukan angka dilewati.</summary>
        public static IReadOnlyList<int> ParseDays(string? csv) =>
            (csv ?? "").Split(',').Select(s => int.TryParse(s.Trim(), out var d) ? d : 0).Where(d => d > 0).ToList();
    }
}
