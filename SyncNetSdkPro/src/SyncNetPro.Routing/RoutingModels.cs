namespace SyncNetPro.Routing;

/// <summary>Status kesehatan satu supplier/biller (baris <c>sw_routes_supplier_status</c>).</summary>
public sealed class SupplierHealth
{
    /// <summary>Nama node supplier (<c>sw_nodes.node_name</c>).</summary>
    public required string SupplierId { get; init; }

    /// <summary>Status.</summary>
    public SupplierHealthStatus Status { get; set; } = SupplierHealthStatus.Active;

    /// <summary>RC terakhir yang mengubah status/counter.</summary>
    public string? LastRcCode { get; set; }

    /// <summary>Counter timeout berturut-turut.</summary>
    public int ConsecutiveTimeoutCount { get; set; }

    /// <summary>Counter rc gagal berturut-turut.</summary>
    public int ConsecutiveFailedCount { get; set; }

    /// <summary>Counter pending berturut-turut.</summary>
    public int ConsecutivePendingCount { get; set; }

    /// <summary>Counter respons lambat berturut-turut.</summary>
    public int ConsecutiveLatencyCount { get; set; }

    /// <summary>Alasan blokir (<see cref="BlockReasons"/>).</summary>
    public string? BlockReason { get; set; }

    /// <summary>Latency terakhir (ms).</summary>
    public int? LastLatencyMs { get; set; }

    /// <summary>Jumlah transaksi uji yang gagal sejak blokir pertama.</summary>
    public int RetryCount { get; set; }

    /// <summary>Waktu transaksi terakhir yang mengubah status.</summary>
    public DateTime? LastTransactionAt { get; set; }

    /// <summary>Awal gangguan.</summary>
    public DateTime? BlockedSince { get; set; }

    /// <summary>Akhir cooldown; <c>null</c> saat diblokir = hanya reset manual.</summary>
    public DateTime? BlockedUntil { get; set; }

    /// <summary>Diubah oleh.</summary>
    public string? UpdatedBy { get; set; }

    /// <summary>Waktu diubah.</summary>
    public DateTime? UpdatedAt { get; set; }

    internal SupplierHealth Clone() => (SupplierHealth)MemberwiseClone();

    internal bool SameState(SupplierHealth other) =>
        Status == other.Status && BlockReason == other.BlockReason
        && ConsecutiveTimeoutCount == other.ConsecutiveTimeoutCount && ConsecutiveFailedCount == other.ConsecutiveFailedCount
        && ConsecutivePendingCount == other.ConsecutivePendingCount && ConsecutiveLatencyCount == other.ConsecutiveLatencyCount
        && RetryCount == other.RetryCount && BlockedSince == other.BlockedSince && BlockedUntil == other.BlockedUntil;
}

/// <summary>Konfigurasi health check (<c>sw_routes_failover_config</c>). Kategori timeout memakai kolom <c>*_suspect</c>.</summary>
public sealed record FailoverConfig
{
    /// <summary>Kategori routing.</summary>
    public required string RoutingType { get; init; }

    /// <summary>Produk; <c>null</c> = config global kategori (juga saklar darurat).</summary>
    public string? InstId { get; init; }

    /// <summary>RC link down (CSV).</summary>
    public string? RcLinkDown { get; init; }

    /// <summary>Cooldown link down (menit); <c>null</c> = reset manual.</summary>
    public int? LinkDownCooldownMinutes { get; init; }

    /// <summary>RC timeout (CSV).</summary>
    public string? RcTimeout { get; init; }

    /// <summary>Batas timeout berturut-turut.</summary>
    public int MaxConsecutiveTimeout { get; init; }

    /// <summary>Cooldown timeout (menit).</summary>
    public int? TimeoutCooldownMinutes { get; init; }

    /// <summary>RC gagal (CSV); kosong = kategori nonaktif.</summary>
    public string? RcFailed { get; init; }

    /// <summary>Batas rc gagal berturut-turut.</summary>
    public int MaxConsecutiveFailed { get; init; } = 3;

    /// <summary>Cooldown rc gagal (menit).</summary>
    public int? FailedCooldownMinutes { get; init; }

    /// <summary>RC pending (CSV); kosong = kategori nonaktif.</summary>
    public string? RcPending { get; init; }

    /// <summary>Batas pending berturut-turut.</summary>
    public int MaxConsecutivePending { get; init; } = 5;

    /// <summary>Cooldown pending (menit).</summary>
    public int? PendingCooldownMinutes { get; init; }

    /// <summary>Ambang latency (ms); kosong/0 = kategori nonaktif.</summary>
    public int? LatencyThresholdMs { get; init; }

    /// <summary>Batas respons lambat berturut-turut.</summary>
    public int MaxConsecutiveLatency { get; init; } = 3;

    /// <summary>Cooldown latency (menit).</summary>
    public int? LatencyCooldownMinutes { get; init; }

    /// <summary>Baris aktif (baris global nonaktif = failover kategori dimatikan).</summary>
    public bool IsActive { get; init; }

    internal bool IsLinkDownCode(string? rc) => ContainsCode(RcLinkDown, rc);

    internal bool IsTimeoutCode(string? rc) => ContainsCode(RcTimeout, rc);

    internal bool IsFailedCode(string? rc) => ContainsCode(RcFailed, rc);

    internal bool IsPendingCode(string? rc) => ContainsCode(RcPending, rc);

    internal bool IsLatencyActive => LatencyThresholdMs is > 0;

    internal int? CooldownFor(string reason) => reason switch
    {
        BlockReasons.LinkDown => LinkDownCooldownMinutes,
        BlockReasons.Timeout => TimeoutCooldownMinutes,
        BlockReasons.RcFailed => FailedCooldownMinutes,
        BlockReasons.Pending => PendingCooldownMinutes,
        BlockReasons.Latency => LatencyCooldownMinutes,
        _ => null,
    };

    private static bool ContainsCode(string? csv, string? rc)
    {
        if (string.IsNullOrEmpty(csv) || string.IsNullOrEmpty(rc)) return false;
        string code = rc.Trim();
        foreach (string item in csv.Split(',')) if (item.Trim() == code) return true;
        return false;
    }
}

/// <summary>Satu aturan Jadwal Routing (<c>sw_routes_schedule</c>, hanya aturan aktif).</summary>
public sealed record ScheduleRule
{
    /// <summary>Id aturan.</summary>
    public int Id { get; init; }

    /// <summary>Nama aturan.</summary>
    public string? Name { get; init; }

    /// <summary><see cref="ScheduleRuleTypes"/>.</summary>
    public required string RuleType { get; init; }

    /// <summary>Kategori; <c>null</c> = keduanya.</summary>
    public string? RoutingType { get; init; }

    /// <summary>Produk; <c>null</c> = semua produk biller.</summary>
    public string? InstId { get; init; }

    /// <summary>Node biller.</summary>
    public required string NodeName { get; init; }

    /// <summary>Priority (Prioritas Jadwal).</summary>
    public short? Priority { get; init; }

    /// <summary><see cref="ScheduleRecurrences"/>.</summary>
    public required string Recurrence { get; init; }

    /// <summary>Mulai (aturan Sekali).</summary>
    public DateTime? StartAt { get; init; }

    /// <summary>Selesai eksklusif (aturan Sekali).</summary>
    public DateTime? EndAt { get; init; }

    /// <summary>Jam mulai (aturan berulang).</summary>
    public TimeSpan? TimeStart { get; init; }

    /// <summary>Jam selesai eksklusif; lebih kecil dari mulai = lewat tengah malam; sama = 24 jam.</summary>
    public TimeSpan? TimeEnd { get; init; }

    /// <summary>Mingguan: 1=Senin..7=Minggu; bulanan: tanggal.</summary>
    public IReadOnlyList<int> Days { get; init; } = [];

    /// <summary>Berlaku mulai (tanggal).</summary>
    public DateTime? ValidFrom { get; init; }

    /// <summary>Berlaku sampai (tanggal).</summary>
    public DateTime? ValidUntil { get; init; }

    /// <summary>Aturan Sekali.</summary>
    public bool IsOnce => Recurrence == ScheduleRecurrences.Once;
}

/// <summary>Aturan Volume &amp; Tiering (<c>sw_routes_commitment</c>).</summary>
public sealed record CommitmentRule
{
    /// <summary>Id.</summary>
    public int Id { get; init; }

    /// <summary>Nama.</summary>
    public string? Name { get; init; }

    /// <summary><see cref="CommitmentConstants.Limit"/> / <see cref="CommitmentConstants.Target"/>.</summary>
    public required string RuleType { get; init; }

    /// <summary>Kategori.</summary>
    public required string RoutingType { get; init; }

    /// <summary>Node biller.</summary>
    public required string NodeName { get; init; }

    /// <summary>Produk; <c>null</c> = semua produk biller pada kategori ini.</summary>
    public string? InstId { get; init; }

    /// <summary><see cref="CommitmentConstants.Count"/> / <see cref="CommitmentConstants.Amount"/>.</summary>
    public required string Metric { get; init; }

    /// <summary><see cref="CommitmentConstants.Daily"/> / <see cref="CommitmentConstants.Monthly"/>.</summary>
    public required string PeriodType { get; init; }

    /// <summary>Nilai kuota/target.</summary>
    public decimal Threshold { get; init; }

    /// <summary>Persen peringatan kuota (default 80).</summary>
    public short? WarnPct { get; init; }

    /// <summary>Berlaku mulai (tanggal).</summary>
    public DateTime? ValidFrom { get; init; }

    /// <summary>Berlaku sampai (tanggal).</summary>
    public DateTime? ValidUntil { get; init; }

    /// <summary>Dibuat.</summary>
    public DateTime? CreatedAt { get; init; }

    /// <summary>Berlaku pada hari tersebut.</summary>
    public bool IsValidOn(DateTime day) =>
        (ValidFrom is null || ValidFrom.Value.Date <= day.Date) && (ValidUntil is null || ValidUntil.Value.Date >= day.Date);

    /// <summary>Aturan berlaku untuk kategori + biller + produk.</summary>
    public bool InScope(string routingType, string nodeName, string productId) =>
        RoutingType == routingType && NodeName == nodeName && (InstId is null || InstId == productId);

    /// <summary>Awal periode yang memuat <paramref name="day"/>.</summary>
    public DateTime PeriodStart(DateTime day) => PeriodType == CommitmentConstants.Monthly ? new DateTime(day.Year, day.Month, 1, 0, 0, 0, day.Kind) : day.Date;
}

/// <summary>Volume harian biller + produk (<c>sw_routes_volume</c>); juga delta saat flush.</summary>
public sealed record VolumeDelta(DateTime Date, string RoutingType, string NodeName, string InstId, long Count, decimal Amount);

/// <summary>Total volume biller + produk relatif tanggal muat.</summary>
public sealed record VolumeTotal
{
    /// <summary>Kategori.</summary>
    public required string RoutingType { get; init; }

    /// <summary>Node.</summary>
    public required string NodeName { get; init; }

    /// <summary>Produk.</summary>
    public required string InstId { get; init; }

    /// <summary>Jumlah hari ini.</summary>
    public long TodayCount { get; init; }

    /// <summary>Nominal hari ini.</summary>
    public decimal TodayAmount { get; init; }

    /// <summary>Jumlah kemarin.</summary>
    public long YesterdayCount { get; init; }

    /// <summary>Nominal kemarin.</summary>
    public decimal YesterdayAmount { get; init; }

    /// <summary>Jumlah bulan ini s.d. hari ini.</summary>
    public long MonthCount { get; init; }

    /// <summary>Nominal bulan ini s.d. hari ini.</summary>
    public decimal MonthAmount { get; init; }

    /// <summary>Jumlah bulan lalu.</summary>
    public long PreviousMonthCount { get; init; }

    /// <summary>Nominal bulan lalu.</summary>
    public decimal PreviousMonthAmount { get; init; }
}

/// <summary>Event Volume &amp; Tiering (<c>sw_routes_commitment_log</c>).</summary>
public sealed record CommitmentEvent
{
    /// <summary>Id aturan.</summary>
    public int CommitmentId { get; init; }

    /// <summary>Tipe aturan.</summary>
    public required string RuleType { get; init; }

    /// <summary>Kategori.</summary>
    public required string RoutingType { get; init; }

    /// <summary>Node.</summary>
    public required string NodeName { get; init; }

    /// <summary>Produk.</summary>
    public string? InstId { get; init; }

    /// <summary>Metrik.</summary>
    public required string Metric { get; init; }

    /// <summary>Periode.</summary>
    public required string PeriodType { get; init; }

    /// <summary>Awal periode.</summary>
    public DateTime PeriodStart { get; init; }

    /// <summary>Event (<see cref="CommitmentConstants"/>).</summary>
    public required string Event { get; init; }

    /// <summary>Volume saat event.</summary>
    public decimal Volume { get; init; }

    /// <summary>Nilai aturan.</summary>
    public decimal Threshold { get; init; }

    /// <summary>Waktu.</summary>
    public DateTime CreatedAt { get; init; }
}

/// <summary>Biller yang dipilih untuk satu siklus transaksi (<c>sw_routes_tran_map</c>).</summary>
public sealed record RoutingCycle
{
    /// <summary>Id baris.</summary>
    public long Id { get; init; }

    /// <summary>Merchant (CA).</summary>
    public string? MerchantId { get; init; }

    /// <summary>Terminal.</summary>
    public string? TerminalId { get; init; }

    /// <summary>Refnum (sama untuk inquiry, payment, advice).</summary>
    public string? Refnum { get; init; }

    /// <summary>Kategori.</summary>
    public required string RoutingType { get; init; }

    /// <summary>Produk.</summary>
    public string? InstId { get; init; }

    /// <summary>Denom (topup).</summary>
    public int? Denom { get; init; }

    /// <summary>Biller siklus.</summary>
    public required string NodeName { get; init; }

    /// <summary>Switch key inquiry.</summary>
    public string? InquirySwitchKey { get; init; }

    /// <summary>Switch key payment.</summary>
    public string? SwitchKey { get; init; }
}

/// <summary>Harga supplier untuk produk + denom (<c>sw_margin_supplier</c>).</summary>
public sealed record SupplierPrice(string SupplierId, string ProductId, int Denom, int PurchasePrice, int SellingPrice, int Margin, int? Priority, int LbWeight, bool IsActive);

/// <summary>Harga jual khusus merchant (<c>sw_margin_merchant</c>).</summary>
public sealed record MerchantPrice(string MerchantId, string ProductId, int Denom, int SellingPrice);

/// <summary>Biller produk: primary (<c>sw_routes_by_inst</c>, <see cref="Priority"/> = 1) atau alternate (<c>sw_routes_by_inst_alt</c>).</summary>
public sealed record ProductRouteEntry(string ProductId, int NodeId, string NodeName, int Priority, int? FeeSharing, int LbWeight);

/// <summary>Produk topup yang dirutekan by margin (<c>sw_routes_margin</c>).</summary>
public sealed record MarginRouteEntry(string ProductId, string? RoutingMode, string? StaticNodeName);

/// <summary>Baris Product Fees (<c>sw_fees</c>).</summary>
public sealed record FeeRule
{
    /// <summary>Produk.</summary>
    public required string ProductId { get; init; }

    /// <summary>CA; kosong = baris default produk.</summary>
    public string? MerchantId { get; init; }

    /// <summary>Sub CA.</summary>
    public string? SubMerchantId { get; init; }

    /// <summary>Fee tetap (<c>fee_type = 0</c>) atau persen.</summary>
    public bool IsFixedFee { get; init; }

    /// <summary>Routing mode; kosong di baris CA = ikuti baris default produk.</summary>
    public string? RoutingMode { get; init; }

    /// <summary>Biller pilihan untuk mode STATIC.</summary>
    public int? StaticNodeId { get; init; }

    /// <summary>Total admin fee.</summary>
    public int FixedFeeTotal { get; init; }

    /// <summary>Fee loket.</summary>
    public int FixedFeeAcquirer { get; init; }

    /// <summary>Fee mitra.</summary>
    public int FixedFeeMerchant { get; init; }

    /// <summary>Fee issuer.</summary>
    public int FixedFeeIssuer { get; init; }

    /// <summary>Fee biller (STATIC).</summary>
    public int FixedFeeBiller { get; init; }

    /// <summary>Fee perusahaan (STATIC).</summary>
    public int FixedFeeSwitch { get; init; }

    /// <summary>Persen total dari nominal.</summary>
    public decimal PercentFeeTotal { get; init; }

    /// <summary>Persen loket dari total fee.</summary>
    public decimal PercentFeeAcquirer { get; init; }

    /// <summary>Persen mitra dari total fee.</summary>
    public decimal PercentFeeMerchant { get; init; }

    /// <summary>Persen issuer dari total fee.</summary>
    public decimal PercentFeeIssuer { get; init; }

    /// <summary>Persen biller dari total fee.</summary>
    public decimal PercentFeeBiller { get; init; }

    /// <summary>Persen perusahaan dari total fee.</summary>
    public decimal PercentFeeSwitch { get; init; }
}
