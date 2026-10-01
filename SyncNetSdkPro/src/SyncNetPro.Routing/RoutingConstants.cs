namespace SyncNetPro.Routing;

/// <summary>
/// Cara memilih biller (<c>sw_fees.routing_mode</c> untuk bill payment/purchase, <c>sw_routes_margin.routing_mode</c> untuk topup).
/// </summary>
public static class RoutingModes
{
    /// <summary>Satu biller tetap, tanpa failover.</summary>
    public const string Static = "STATIC";

    /// <summary>Priority terkecil yang sehat.</summary>
    public const string Priority = "PRIORITY";

    /// <summary>Bill payment: sharing fee terbesar; topup: margin terbesar.</summary>
    public const string BestPrice = "BEST_PRICE";

    /// <summary>Acak berbobot di antara biller yang sehat.</summary>
    public const string LoadBalance = "LOAD_BALANCE";

    /// <summary>Mode dynamic = memilih biller per siklus dan melewati biller yang diblokir.</summary>
    public static bool IsDynamic(string? mode) => mode is Priority or BestPrice or LoadBalance;

    /// <summary>Nilai kosong atau tidak dikenal diperlakukan sebagai <see cref="Static"/>.</summary>
    public static string Normalize(string? mode) => IsDynamic(mode) ? mode! : Static;
}

/// <summary>Kategori routing (health check, jadwal, volume dilacak per kategori).</summary>
public static class RoutingTypes
{
    /// <summary>Topup (routing by margin).</summary>
    public const string Margin = "MARGIN";

    /// <summary>Bill payment &amp; purchase (routing by product).</summary>
    public const string Product = "PRODUCT";
}

/// <summary>Status kesehatan supplier (<c>sw_routes_supplier_status.status</c>).</summary>
public enum SupplierHealthStatus
{
    /// <summary>Boleh dipilih.</summary>
    Active,

    /// <summary>Link down.</summary>
    Down,

    /// <summary>Timeout/rc gagal/pending/latency berturut-turut.</summary>
    Suspect,
}

/// <summary>Alasan blokir (<c>block_reason</c>, <c>sw_routes_failover_log.reason</c>).</summary>
public static class BlockReasons
{
    /// <summary>Link down.</summary>
    public const string LinkDown = "LINK_DOWN";

    /// <summary>Timeout.</summary>
    public const string Timeout = "TIMEOUT";

    /// <summary>RC gagal.</summary>
    public const string RcFailed = "RC_FAILED";

    /// <summary>Pending.</summary>
    public const string Pending = "PENDING";

    /// <summary>Latency di atas ambang.</summary>
    public const string Latency = "LATENCY";

    /// <summary>Transaksi ditolak Jadwal Routing (hanya failover log, status tidak berubah).</summary>
    public const string ScheduleClosed = "SCHEDULE_CLOSED";
}

/// <summary>Konstanta Jadwal Routing (<c>sw_routes_schedule</c>).</summary>
public static class ScheduleRuleTypes
{
    /// <summary>Tutup.</summary>
    public const string Closed = "CLOSED";

    /// <summary>Jam Operasional.</summary>
    public const string Open = "OPEN";

    /// <summary>Prioritas Jadwal.</summary>
    public const string Priority = "PRIORITY";
}

/// <summary>Pola pengulangan Jadwal Routing.</summary>
public static class ScheduleRecurrences
{
    /// <summary>Sekali (start_dt–end_dt).</summary>
    public const string Once = "ONCE";

    /// <summary>Harian.</summary>
    public const string Daily = "DAILY";

    /// <summary>Mingguan (1=Senin..7=Minggu).</summary>
    public const string Weekly = "WEEKLY";

    /// <summary>Bulanan (tanggal).</summary>
    public const string Monthly = "MONTHLY";
}

/// <summary>Konstanta Volume &amp; Tiering (<c>sw_routes_commitment</c>).</summary>
public static class CommitmentConstants
{
    /// <summary>Kuota: biller dibuang bila volume ≥ nilai.</summary>
    public const string Limit = "LIMIT";

    /// <summary>Target Tier: biller didahulukan selama volume &lt; nilai.</summary>
    public const string Target = "TARGET";

    /// <summary>Metrik jumlah transaksi.</summary>
    public const string Count = "COUNT";

    /// <summary>Metrik nominal.</summary>
    public const string Amount = "AMOUNT";

    /// <summary>Periode harian.</summary>
    public const string Daily = "DAILY";

    /// <summary>Periode bulanan.</summary>
    public const string Monthly = "MONTHLY";

    /// <summary>Event: volume mencapai persentase peringatan kuota.</summary>
    public const string LimitWarn = "LIMIT_WARN";

    /// <summary>Event: kuota tercapai.</summary>
    public const string LimitReached = "LIMIT_REACHED";

    /// <summary>Event: semua kandidat habis kuota sehingga kuota diabaikan.</summary>
    public const string LimitIgnored = "LIMIT_IGNORED";

    /// <summary>Event: target tercapai.</summary>
    public const string TargetReached = "TARGET_REACHED";

    /// <summary>Event: target periode lalu tidak tercapai.</summary>
    public const string TargetMissed = "TARGET_MISSED";
}
