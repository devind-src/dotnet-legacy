namespace SyncNetPro.Routing;

/// <summary>Hasil pemilihan biller untuk siklus baru.</summary>
/// <param name="NodeName">Biller; saat ditolak tetap kandidat pertama agar advice/reversal jalur fallback punya tujuan.</param>
/// <param name="Rejected">Tidak ada biller buka menurut Jadwal Routing.</param>
/// <param name="ScheduleId">Aturan jadwal penyebab.</param>
public sealed record RouteSelection(string NodeName, bool Rejected = false, int? ScheduleId = null)
{
    /// <summary>Ke biller.</summary>
    public static RouteSelection To(string? node) => new(node ?? string.Empty);

    /// <summary>Ditolak jadwal.</summary>
    public static RouteSelection Reject(string? fallbackNode, int? scheduleId) => new(fallbackNode ?? string.Empty, true, scheduleId);
}

/// <summary>Pilih acak berbobot (mode LOAD_BALANCE); stateless dan aman lintas request.</summary>
internal static class WeightedPicker
{
    public static T? Pick<T>(IReadOnlyList<T> items, Func<T, int> weight, Random random)
        where T : class
    {
        int total = items.Sum(i => Math.Max(0, weight(i)));
        if (total <= 0) return null;

        int roll = random.Next(total);
        foreach (T item in items)
        {
            int w = Math.Max(0, weight(item));
            if (roll < w) return item;
            roll -= w;
        }

        return null;
    }
}

/// <summary>Routing topup statis per produk (<c>sw_routes_by_inst</c> primary) — <c>StaticRoutingStrategy</c> SDK lama.</summary>
public sealed class StaticRouting(IRoutingDataStore store)
{
    private volatile Dictionary<string, string> _routes = [];

    /// <summary>Muat route (diganti utuh; SDK lama mengosongkan di tempat).</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var routes = new Dictionary<string, string>();
        foreach (ProductRouteEntry r in await store.GetProductRoutesAsync(cancellationToken).ConfigureAwait(false)) routes.TryAdd(r.ProductId, r.NodeName);
        _routes = routes;
    }

    /// <summary>Produk punya route statis.</summary>
    public bool IsApplicable(string productId) => _routes.ContainsKey(productId);

    /// <summary>Supplier statis, atau <c>null</c>.</summary>
    public string? ResolveSupplier(string productId) => _routes.GetValueOrDefault(productId);
}

/// <summary>
/// Routing topup by margin (<c>sw_routes_margin</c>) — <c>MarginRoutingStrategy</c> SDK lama. Kandidat = supplier aktif per
/// produk + denom. STATIC: supplier pilihan (kosong = margin terbesar) tanpa failover; BEST_PRICE: margin terbesar;
/// PRIORITY: priority terkecil; LOAD_BALANCE: acak berbobot. Mode dynamic melewati supplier DOWN/SUSPECT (semua
/// diblokir = urutan pertama), menerapkan Jadwal Routing lalu Volume &amp; Tiering.
/// </summary>
public sealed class MarginRouting(IRoutingDataStore store, PriceBook prices, SupplierHealthTracker health, RoutingSchedule schedule,
    CommitmentTracker? commitment = null, Random? random = null)
{
    private readonly Random _random = random ?? Random.Shared;
    private volatile Dictionary<string, MarginRouteEntry> _routes = [];

    /// <summary>Muat produk routing margin.</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var routes = new Dictionary<string, MarginRouteEntry>();
        foreach (MarginRouteEntry r in await store.GetMarginRoutesAsync(cancellationToken).ConfigureAwait(false))
        {
            routes.TryAdd(r.ProductId, r with { RoutingMode = RoutingModes.Normalize(r.RoutingMode), StaticNodeName = string.IsNullOrEmpty(r.StaticNodeName) ? null : r.StaticNodeName });
        }

        _routes = routes;
    }

    /// <summary>Produk dirutekan by margin.</summary>
    public bool IsApplicable(string productId) => _routes.ContainsKey(productId);

    /// <summary>Failover aktif: mode dynamic dan tombol darurat kategori topup tidak dimatikan.</summary>
    public bool IsFailoverActive(string productId) =>
        _routes.TryGetValue(productId, out MarginRouteEntry? r) && RoutingModes.IsDynamic(r.RoutingMode) && health.IsFailoverEnabled(RoutingTypes.Margin);

    /// <summary>Pilih supplier untuk siklus baru.</summary>
    public RouteSelection SelectSupplier(string productId, long denom)
    {
        if (!prices.TryGetRankedSuppliers(productId, denom, out IReadOnlyList<SupplierPrice> ranked) || ranked.Count == 0) return RouteSelection.To(string.Empty);

        _routes.TryGetValue(productId, out MarginRouteEntry? route);
        string mode = route?.RoutingMode ?? RoutingModes.Static;
        const string rt = RoutingTypes.Margin;

        if (mode == RoutingModes.Static)
        {
            // Supplier pilihan hanya dipakai bila punya harga aktif untuk denom ini.
            string? fixedSupplier = route?.StaticNodeName;
            string target = fixedSupplier is not null && ranked.Any(p => p.SupplierId == fixedSupplier) ? fixedSupplier : ranked[0].SupplierId;
            ScheduleRule? closedBy = health.IsFailoverEnabled(rt) ? schedule.ClosedBy(rt, productId, target, schedule.Now) : null;
            return closedBy is not null ? RouteSelection.Reject(target, closedBy.Id) : RouteSelection.To(target);
        }

        List<SupplierPrice> ordered = Order(ranked, mode);

        // Tombol darurat dimatikan: selalu urutan pertama, status dan jadwal diabaikan.
        if (!IsFailoverActive(productId)) return RouteSelection.To(ordered[0].SupplierId);

        ScheduleOrder<SupplierPrice> sched = schedule.Apply(ordered, p => p.SupplierId, rt, productId);
        if (sched.AllClosed) return RouteSelection.Reject(ordered[0].SupplierId, sched.ClosedBy?.Id);

        DateTime now = schedule.Now;
        bool IsScheduled(SupplierPrice p) => schedule.SchedulePriority(rt, productId, p.SupplierId, now).HasValue;

        IReadOnlyList<SupplierPrice> open = sched.Open;
        IReadOnlySet<string> targeted = new HashSet<string>();
        if (commitment is not null)
        {
            CommitmentOrder<SupplierPrice> commit = commitment.Apply(open, p => p.SupplierId, IsScheduled, rt, productId);
            open = commit.Open;
            targeted = commit.Targeted;
        }

        if (mode == RoutingModes.LoadBalance)
        {
            // Prioritas Jadwal / Target Tier yang sehat didahulukan; selain itu acak berbobot.
            var eligible = open.Where(p => health.IsEligible(p.SupplierId)).ToList();
            SupplierPrice? preferred = eligible.FirstOrDefault(p => IsScheduled(p) || targeted.Contains(p.SupplierId));
            if (preferred is not null) return RouteSelection.To(preferred.SupplierId);
            SupplierPrice? picked = WeightedPicker.Pick(eligible, p => p.LbWeight, _random);
            if (picked is not null) return RouteSelection.To(picked.SupplierId);
        }

        // Turun menurut urutan, lewati DOWN/SUSPECT; semua diblokir = urutan pertama yang buka.
        return RouteSelection.To(open.FirstOrDefault(p => health.IsEligible(p.SupplierId))?.SupplierId ?? open[0].SupplierId);
    }

    // ranked sudah urut margin; PRIORITY/LOAD_BALANCE mengurutkan ulang menurut priority (kosong di akhir), tie-break margin.
    private static List<SupplierPrice> Order(IReadOnlyList<SupplierPrice> ranked, string mode) =>
        mode == RoutingModes.BestPrice
            ? [.. ranked]
            : [.. ranked.Select((p, rank) => (p, rank)).OrderBy(x => x.p.Priority ?? int.MaxValue).ThenBy(x => x.rank).Select(x => x.p)];
}

/// <summary>
/// Routing bill payment &amp; purchase (<c>ProductRoutingStrategy</c> SDK lama). Kandidat = primary lalu alternate menurut
/// priority. PRIORITY: primary-backup; BEST_PRICE: sharing fee terbesar (tie-break priority); LOAD_BALANCE: acak berbobot.
/// STATIC ditangani <see cref="RoutingResolver"/>.
/// </summary>
public sealed class ProductRouting(IRoutingDataStore store, SupplierHealthTracker health, RoutingSchedule schedule,
    CommitmentTracker? commitment = null, Random? random = null)
{
    private readonly Random _random = random ?? Random.Shared;
    private volatile Dictionary<string, List<ProductRouteEntry>> _routes = [];

    /// <summary>Muat primary + alternate.</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var routes = new Dictionary<string, List<ProductRouteEntry>>();
        foreach (ProductRouteEntry r in await store.GetProductRoutesAsync(cancellationToken).ConfigureAwait(false))
        {
            routes.TryAdd(r.ProductId, [r with { Priority = 1 }]);
        }

        // Alternate tanpa primary diabaikan; node yang sudah menjadi kandidat dilewati.
        foreach (ProductRouteEntry alt in await store.GetAlternateProductRoutesAsync(cancellationToken).ConfigureAwait(false))
        {
            if (routes.TryGetValue(alt.ProductId, out List<ProductRouteEntry>? candidates) && !candidates.Any(c => c.NodeName == alt.NodeName))
            {
                candidates.Add(alt);
            }
        }

        _routes = routes;
    }

    /// <summary>Produk punya route.</summary>
    public bool HasRoute(string productId) => _routes.ContainsKey(productId);

    /// <summary>Primary biller.</summary>
    public string? GetPrimary(string productId) => _routes.TryGetValue(productId, out List<ProductRouteEntry>? c) && c.Count > 0 ? c[0].NodeName : null;

    /// <summary>Node untuk mode STATIC (harus salah satu biller produk ini).</summary>
    public string? GetNodeName(string productId, int nodeId) =>
        _routes.TryGetValue(productId, out List<ProductRouteEntry>? c) ? c.FirstOrDefault(x => x.NodeId == nodeId)?.NodeName : null;

    /// <summary>Sharing fee biller untuk produk (<c>null</c> = belum diisi).</summary>
    public int? GetSharingFee(string productId, string? nodeName) =>
        _routes.TryGetValue(productId, out List<ProductRouteEntry>? c) ? c.FirstOrDefault(x => x.NodeName == nodeName)?.FeeSharing : null;

    /// <summary>Routing dynamic aktif: ada route, mode dynamic, tombol darurat bill payment tidak dimatikan.</summary>
    public bool IsDynamicActive(string productId, string mode) =>
        HasRoute(productId) && RoutingModes.IsDynamic(mode) && health.IsFailoverEnabled(RoutingTypes.Product);

    /// <summary>
    /// Pilihan siklus baru: buang yang tutup (jadwal), buang yang kuotanya habis, lalu kandidat eligible pertama menurut
    /// Prioritas Jadwal → Target Tier → mode. Semua diblokir = kandidat pertama; tidak ada yang buka = ditolak.
    /// </summary>
    public RouteSelection SelectNode(string productId, string mode)
    {
        if (!_routes.TryGetValue(productId, out List<ProductRouteEntry>? candidates) || candidates.Count == 0) return RouteSelection.To(string.Empty);

        const string rt = RoutingTypes.Product;
        List<ProductRouteEntry> ordered = Order(candidates, mode);
        ScheduleOrder<ProductRouteEntry> sched = schedule.Apply(ordered, c => c.NodeName, rt, productId);
        if (sched.AllClosed) return RouteSelection.Reject(candidates[0].NodeName, sched.ClosedBy?.Id);

        DateTime now = schedule.Now;
        bool IsScheduled(ProductRouteEntry c) => schedule.SchedulePriority(rt, productId, c.NodeName, now).HasValue;

        IReadOnlyList<ProductRouteEntry> open = sched.Open;
        IReadOnlySet<string> targeted = new HashSet<string>();
        if (commitment is not null)
        {
            CommitmentOrder<ProductRouteEntry> commit = commitment.Apply(open, c => c.NodeName, IsScheduled, rt, productId);
            open = commit.Open;
            targeted = commit.Targeted;
        }

        if (mode == RoutingModes.LoadBalance)
        {
            var eligible = open.Where(c => health.IsEligible(c.NodeName)).ToList();
            ProductRouteEntry? preferred = eligible.FirstOrDefault(c => IsScheduled(c) || targeted.Contains(c.NodeName));
            if (preferred is not null) return RouteSelection.To(preferred.NodeName);
            ProductRouteEntry? picked = WeightedPicker.Pick(eligible, c => c.LbWeight, _random);
            if (picked is not null) return RouteSelection.To(picked.NodeName);
        }

        return RouteSelection.To(open.FirstOrDefault(c => health.IsEligible(c.NodeName))?.NodeName ?? open[0].NodeName);
    }

    // BEST_PRICE: biller tanpa sharing fee paling akhir.
    private static List<ProductRouteEntry> Order(List<ProductRouteEntry> candidates, string mode) =>
        mode == RoutingModes.BestPrice
            ? [.. candidates.OrderByDescending(c => c.FeeSharing.HasValue).ThenByDescending(c => c.FeeSharing ?? 0).ThenBy(c => c.Priority)]
            : [.. candidates.OrderBy(c => c.Priority)];
}
