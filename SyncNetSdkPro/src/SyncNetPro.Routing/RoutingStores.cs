namespace SyncNetPro.Routing;

/// <summary>Penyimpanan status kesehatan supplier dan failover log.</summary>
public interface ISupplierHealthStore
{
    /// <summary>Semua status supplier.</summary>
    Task<IReadOnlyList<SupplierHealth>> GetSupplierHealthAsync(CancellationToken cancellationToken);

    /// <summary>Semua config health check.</summary>
    Task<IReadOnlyList<FailoverConfig>> GetFailoverConfigAsync(CancellationToken cancellationToken);

    /// <summary>Simpan status (insert/update).</summary>
    Task UpsertSupplierHealthAsync(SupplierHealth health, CancellationToken cancellationToken);

    /// <summary>Tambah baris failover log.</summary>
    Task InsertFailoverLogAsync(FailoverLogEntry entry, CancellationToken cancellationToken);
}

/// <summary>Baris <c>sw_routes_failover_log</c>.</summary>
public sealed record FailoverLogEntry(string RoutingType, string? InstId, long? Denom, string? TraceNumber, string? FromSupplierId,
    string? ToSupplierId, string? RcCode, string Reason, int? LatencyMs, int? ScheduleId, DateTime CreatedAt);

/// <summary>Sumber aturan Jadwal Routing.</summary>
public interface IRoutingScheduleStore
{
    /// <summary>Aturan yang masih dapat berlaku pada <paramref name="now"/>.</summary>
    Task<IReadOnlyList<ScheduleRule>> GetScheduleRulesAsync(DateTime now, CancellationToken cancellationToken);
}

/// <summary>Sumber config, aturan, volume, dan log Volume &amp; Tiering.</summary>
public interface ICommitmentStore
{
    /// <summary>Saklar ON/OFF global; tanpa baris/tabel = OFF.</summary>
    Task<bool> IsCommitmentActiveAsync(CancellationToken cancellationToken);

    /// <summary>Aturan aktif (termasuk yang berakhir sejak awal bulan lalu, untuk target terlewat).</summary>
    Task<IReadOnlyList<CommitmentRule>> GetCommitmentRulesAsync(DateTime today, CancellationToken cancellationToken);

    /// <summary>Total per biller + produk relatif <paramref name="today"/>.</summary>
    Task<IReadOnlyList<VolumeTotal>> GetVolumeTotalsAsync(DateTime today, IReadOnlyCollection<string> nodeNames, CancellationToken cancellationToken);

    /// <summary>Tambahkan delta dalam satu transaksi (exception = tidak ada yang tersimpan).</summary>
    Task AddVolumesAsync(IReadOnlyList<VolumeDelta> deltas, CancellationToken cancellationToken);

    /// <summary>Event baru; yang sudah ada diabaikan.</summary>
    Task InsertCommitmentEventsAsync(IReadOnlyList<CommitmentEvent> events, CancellationToken cancellationToken);
}

/// <summary>Catatan siklus transaksi (sticky routing).</summary>
public interface IRoutingCycleStore
{
    /// <summary>Siklus terbaru untuk merchant + terminal + refnum.</summary>
    Task<RoutingCycle?> FindByReferenceAsync(string? merchantId, string? terminalId, string? refnum, CancellationToken cancellationToken);

    /// <summary>Siklus terbaru untuk switch key payment.</summary>
    Task<RoutingCycle?> FindBySwitchKeyAsync(string switchKey, CancellationToken cancellationToken);

    /// <summary>Simpan siklus baru; mengembalikan id.</summary>
    Task<long> InsertAsync(RoutingCycle cycle, CancellationToken cancellationToken);

    /// <summary>Isi switch key payment pada siklus.</summary>
    Task UpdateSwitchKeyAsync(long id, string switchKey, CancellationToken cancellationToken);

    /// <summary>Node tujuan yang dicatat Core untuk transaksi asal (<c>sw_trans_pg.dest_node</c>).</summary>
    Task<string?> FindCoreDestinationAsync(string switchKey, CancellationToken cancellationToken);
}

/// <summary>Data referensi routing &amp; fee (dimuat saat start dan RESYNC).</summary>
public interface IRoutingDataStore
{
    /// <summary>Primary biller per produk (<c>sw_routes_by_inst</c>).</summary>
    Task<IReadOnlyList<ProductRouteEntry>> GetProductRoutesAsync(CancellationToken cancellationToken);

    /// <summary>Alternate biller aktif, urut produk + priority (<c>sw_routes_by_inst_alt</c>).</summary>
    Task<IReadOnlyList<ProductRouteEntry>> GetAlternateProductRoutesAsync(CancellationToken cancellationToken);

    /// <summary>Produk topup routing by margin (<c>sw_routes_margin</c>).</summary>
    Task<IReadOnlyList<MarginRouteEntry>> GetMarginRoutesAsync(CancellationToken cancellationToken);

    /// <summary>Harga supplier (<c>sw_margin_supplier</c>).</summary>
    Task<IReadOnlyList<SupplierPrice>> GetSupplierPricesAsync(CancellationToken cancellationToken);

    /// <summary>Harga jual merchant (<c>sw_margin_merchant</c>).</summary>
    Task<IReadOnlyList<MerchantPrice>> GetMerchantPricesAsync(CancellationToken cancellationToken);

    /// <summary>Product Fees (<c>sw_fees</c>).</summary>
    Task<IReadOnlyList<FeeRule>> GetFeeRulesAsync(CancellationToken cancellationToken);
}
