namespace SyncNetApi.Dtos.RouteFailoverConfigs
{
    /// <summary>Alasan blokir biller (`sw_routes_supplier_status.block_reason`,
    /// `sw_routes_failover_log.reason`). Dipakai bersama backend dan dashboard.</summary>
    public static class HealthCheckReasons
    {
        public const string LinkDown = "LINK_DOWN";
        public const string Timeout = "TIMEOUT";
        public const string RcFailed = "RC_FAILED";
        public const string Pending = "PENDING";
        public const string Latency = "LATENCY";

        /// <summary>Nilai log sebelum Fase 2, sama artinya dengan Timeout.</summary>
        public const string LegacyTimeout = "SUSPECT_TIMEOUT";

        /// <summary>Fase 3: transaksi ditolak (X15) karena semua biller tutup menurut Jadwal Routing.
        /// Hanya di Failover Log, bukan alasan blokir di Supplier Status.</summary>
        public const string ScheduleClosed = "SCHEDULE_CLOSED";

        public static readonly string[] All = [LinkDown, Timeout, RcFailed, Pending, Latency];

        public static string Label(string? reason) => reason switch
        {
            LinkDown => "Link Down",
            Timeout or LegacyTimeout => "Timeout",
            RcFailed => "RC Gagal",
            Pending => "Pending",
            Latency => "Latency",
            ScheduleClosed => "Jadwal (ditolak)",
            null or "" => "-",
            _ => reason
        };
    }
}
