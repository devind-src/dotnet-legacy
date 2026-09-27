namespace SyncNet.Routing
{
    // hasil pemilihan biller untuk siklus baru. Rejected = tidak ada biller yang buka menurut Jadwal
    // Routing (transaksi ditolak di aplikasi channel, tidak dikirim ke core); NodeName tetap diisi
    // kandidat pertama supaya advice/reversal yang jatuh ke jalur fallback tetap punya tujuan.
    public class RouteSelection
    {
        public string NodeName { get; init; } = string.Empty;
        public bool Rejected { get; init; }
        public int? ScheduleId { get; init; }

        public static RouteSelection To(string node) => new() { NodeName = node ?? string.Empty };

        public static RouteSelection Reject(string fallbackNode, int? scheduleId) =>
            new() { NodeName = fallbackNode ?? string.Empty, Rejected = true, ScheduleId = scheduleId };
    }
}
