using Microsoft.Extensions.Logging;
using SyncNetPro.Contracts;

namespace SyncNetPro.Routing;

/// <summary>Data transaksi untuk memilih biller sebuah siklus.</summary>
public sealed record RoutingContext
{
    /// <summary>Tran type.</summary>
    public string? TranType { get; init; }

    /// <summary>Produk (<c>receiving_inst_id</c>).</summary>
    public required string ProductId { get; init; }

    /// <summary>Denom/nominal.</summary>
    public long Denom { get; init; }

    /// <summary>Merchant (CA).</summary>
    public string? MerchantId { get; init; }

    /// <summary>Terminal.</summary>
    public string? TerminalId { get; init; }

    /// <summary>Refnum.</summary>
    public string? Refnum { get; init; }

    /// <summary><c>datetime_tran</c>.</summary>
    public string? TransactionDateTime { get; init; }

    /// <summary>Trace.</summary>
    public string? TraceNumber { get; init; }

    /// <summary>ADVICE/REVERSAL: tran type payment + datetime + trace asal.</summary>
    public string? OriginalData { get; init; }

    /// <summary>Dari request Core.</summary>
    public static RoutingContext From(CoreRequest request, long? denom = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new RoutingContext
        {
            TranType = request.TranType,
            ProductId = request.ReceivingInstitutionId ?? string.Empty,
            Denom = denom ?? (long)request.Amount,
            MerchantId = request.MerchantId,
            TerminalId = request.TerminalId,
            Refnum = request.ReferenceNumber,
            TransactionDateTime = request.TransactionDateTime,
            TraceNumber = request.TraceNumber,
            OriginalData = request.OriginalData,
        };
    }
}

/// <summary>Keputusan routing.</summary>
public sealed record RoutingDecision
{
    /// <summary>RC penolakan Jadwal Routing (prefix X = ditolak interface channel).</summary>
    public const string ScheduleRejectRc = "X15";

    /// <summary>Tidak mengisi <c>sink_node</c>: Core memakai routing statis.</summary>
    public static readonly RoutingDecision None = new();

    /// <summary>Isi <c>private_data.sink_node</c> dengan <see cref="NodeName"/>.</summary>
    public bool Apply { get; init; }

    /// <summary>Biller.</summary>
    public string NodeName { get; init; } = string.Empty;

    /// <summary>Tidak ada biller buka menurut Jadwal Routing: jangan kirim ke Core, balas dengan rc cut-off.</summary>
    public bool Rejected { get; init; }

    /// <summary>Aturan jadwal penyebab.</summary>
    public int? ScheduleId { get; init; }
}

/// <summary>
/// Titik masuk tunggal routing produk (<c>RoutingResolver</c> SDK lama): satu siklus (inquiry → payment → advice/reversal)
/// satu biller. Biller dipilih di transaksi pertama siklus lalu dicatat; transaksi berikutnya mengikuti tanpa memeriksa
/// status maupun jadwal.
/// </summary>
public sealed class RoutingResolver(
    StaticRouting staticRouting,
    MarginRouting marginRouting,
    ProductRouting productRouting,
    SupplierHealthTracker health,
    RoutingSchedule schedule,
    IRoutingCycleStore cycles,
    ILogger<RoutingResolver> logger,
    CommitmentTracker? commitment = null)
{
    /// <summary>Volume &amp; Tiering aktif.</summary>
    public bool IsVolumeActive => commitment?.IsActive == true;

    /// <summary>Muat semua data (start dan RESYNC).</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await health.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await schedule.InitializeAsync(cancellationToken).ConfigureAwait(false);
        if (commitment is not null) await commitment.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await staticRouting.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await marginRouting.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await productRouting.InitializeAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Produk topup dirutekan by margin.</summary>
    public bool IsMarginRouting(string productId) => marginRouting.IsApplicable(productId);

    /// <summary>Produk bill payment/purchase punya route (Routing &gt; Product).</summary>
    public bool HasProductRoute(string productId) => productRouting.HasRoute(productId);

    /// <summary>Primary biller produk.</summary>
    public string? GetProductPrimary(string productId) => productRouting.GetPrimary(productId);

    /// <summary>Sharing fee biller untuk produk (<c>null</c> = belum diisi).</summary>
    public int? GetProductSharingFee(string productId, string? nodeName) => productRouting.GetSharingFee(productId, nodeName);

    /// <summary>Supplier statis topup.</summary>
    public string? ResolveStaticSupplier(string productId) => staticRouting.ResolveSupplier(productId);

    /// <summary>
    /// Topup (routing by margin): selalu lewat <c>sink_node</c>. STATIC / tombol darurat OFF = supplier urutan pertama tanpa failover.
    /// </summary>
    public Task<RoutingDecision> ResolveMarginAsync(RoutingContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return ResolveCycleAsync(RoutingTypes.Margin, context, marginRouting.IsFailoverActive(context.ProductId), applyWhenInactive: true,
            () => marginRouting.SelectSupplier(context.ProductId, context.Denom), cancellationToken);
    }

    /// <summary>
    /// Bill payment &amp; purchase (routing by product); mode dan biller statis dari baris Product Fees CA.
    /// STATIC tanpa biller pilihan = routing statis Core; STATIC dengan biller pilihan = selalu ke biller itu;
    /// dynamic = per siklus (tombol darurat OFF = routing statis Core). Biller statis/primary yang tutup = ditolak.
    /// </summary>
    public async Task<RoutingDecision> ResolveProductAsync(RoutingContext context, string? mode, int? staticNodeId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        string normalized = RoutingModes.Normalize(mode);

        if (normalized == RoutingModes.Static)
        {
            string? node = staticNodeId is int id ? productRouting.GetNodeName(context.ProductId, id) : null;
            string? target = string.IsNullOrEmpty(node) ? productRouting.GetPrimary(context.ProductId) : node;
            RoutingDecision? rejected = await CheckStaticScheduleAsync(RoutingTypes.Product, context, target, cancellationToken).ConfigureAwait(false);
            if (rejected is not null) return rejected;
            return string.IsNullOrEmpty(node) ? RoutingDecision.None : new RoutingDecision { Apply = true, NodeName = node };
        }

        return await ResolveCycleAsync(RoutingTypes.Product, context, productRouting.IsDynamicActive(context.ProductId, normalized), applyWhenInactive: false,
            () => productRouting.SelectNode(context.ProductId, normalized), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Hasil transaksi ke supplier: volume (PAYMENT/ADVICE/REVERSAL sukses, bila fitur ON, apa pun mode) dan health check
    /// (INQUIRY/PAYMENT, bila failover kategori aktif).
    /// </summary>
    public async Task RecordResultAsync(string supplierId, string routingType, string? tranType, string? rcCode, string productId, long? denom,
        string? traceNumber, int? latencyMs = null, decimal? amount = null, string? switchKey = null, string? originalSwitchKey = null,
        DateTime? transactionDate = null, CancellationToken cancellationToken = default)
    {
        RecordVolume(supplierId, routingType, tranType, rcCode, productId, amount ?? denom ?? 0, switchKey, originalSwitchKey, transactionDate);

        if (!health.IsFailoverEnabled(routingType)) return;
        if (tranType is not (TranType.Inquiry or TranType.Payment)) return;

        await health.RecordResponseAsync(supplierId, routingType, tranType == TranType.Payment, rcCode, latencyMs, productId, denom, traceNumber,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Volume saja (transaksi yang dirutekan statis oleh Core; supplier = primary produk).</summary>
    public void RecordVolume(string? supplierId, string routingType, string? tranType, string? rcCode, string productId, decimal amount,
        string? switchKey = null, string? originalSwitchKey = null, DateTime? transactionDate = null) =>
        commitment?.RecordVolume(routingType, supplierId, productId, tranType, rcCode, amount, switchKey, originalSwitchKey, transactionDate);

    /// <summary>Tulis volume yang belum tersimpan (saat berhenti).</summary>
    public Task FlushVolumeAsync(CancellationToken cancellationToken = default) => commitment?.FlushFinalAsync(cancellationToken) ?? Task.CompletedTask;

    private async Task<RoutingDecision> ResolveCycleAsync(string routingType, RoutingContext ctx, bool failoverActive, bool applyWhenInactive,
        Func<RouteSelection> pickNew, CancellationToken cancellationToken)
    {
        if (!failoverActive)
        {
            if (!applyWhenInactive) return RoutingDecision.None;

            // Topup STATIC / tombol darurat OFF: strategi sendiri memutuskan jadwal berlaku atau tidak.
            RouteSelection fixedSelection = pickNew();
            if (fixedSelection.Rejected && await OpensCycleAsync(ctx, cancellationToken).ConfigureAwait(false))
            {
                return await RejectAsync(routingType, ctx, fixedSelection, cancellationToken).ConfigureAwait(false);
            }

            if (!fixedSelection.Rejected) await SaveStaticCycleAsync(routingType, ctx, fixedSelection.NodeName, cancellationToken).ConfigureAwait(false);
            return new RoutingDecision { Apply = true, NodeName = fixedSelection.NodeName };
        }

        string? node = null;
        try
        {
            switch (ctx.TranType)
            {
                case TranType.Inquiry:
                    RouteSelection inquiry = pickNew();
                    if (inquiry.Rejected) return await RejectAsync(routingType, ctx, inquiry, cancellationToken).ConfigureAwait(false);
                    node = inquiry.NodeName;
                    await SaveCycleAsync(routingType, ctx, node, inquiry: true, cancellationToken).ConfigureAwait(false);
                    break;

                case TranType.Payment:
                    RoutingCycle? entry = await cycles.FindByReferenceAsync(ctx.MerchantId, ctx.TerminalId, ctx.Refnum, cancellationToken).ConfigureAwait(false);
                    if (entry is not null)
                    {
                        node = entry.NodeName;
                        await cycles.UpdateSwitchKeyAsync(entry.Id, CurrentSwitchKey(ctx), cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        // Topup tanpa inquiry: transaksi ini yang membuka siklus.
                        RouteSelection payment = pickNew();
                        if (payment.Rejected) return await RejectAsync(routingType, ctx, payment, cancellationToken).ConfigureAwait(false);
                        node = payment.NodeName;
                        await SaveCycleAsync(routingType, ctx, node, inquiry: false, cancellationToken).ConfigureAwait(false);
                    }

                    break;

                case TranType.Advice:
                case TranType.Reversal:
                    node = await FindNodeForOriginalAsync(ctx, cancellationToken).ConfigureAwait(false);
                    break;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Gagal baca/tulis catatan siklus tidak boleh menggagalkan transaksi.
            logger.LogWarning(ex, "Sticky routing refnum {Refnum} gagal; memakai kandidat pertama", ctx.Refnum);
        }

        // Fallback (mis. catatan siklus tidak ditemukan): tidak menolak, kandidat pertama dipakai.
        if (string.IsNullOrEmpty(node)) node = pickNew().NodeName;
        return new RoutingDecision { Apply = true, NodeName = node };
    }

    // ADVICE/REVERSAL: via switch key PAYMENT asal, lalu refnum, lalu node tujuan yang dicatat Core.
    private async Task<string?> FindNodeForOriginalAsync(RoutingContext ctx, CancellationToken cancellationToken)
    {
        string originalKey = SwitchKeys.BuildOriginal(ctx.OriginalData, ctx.TerminalId);
        RoutingCycle? entry = await cycles.FindBySwitchKeyAsync(originalKey, cancellationToken).ConfigureAwait(false);
        if (entry is not null) return entry.NodeName;

        if (!string.IsNullOrEmpty(ctx.Refnum))
        {
            entry = await cycles.FindByReferenceAsync(ctx.MerchantId, ctx.TerminalId, ctx.Refnum, cancellationToken).ConfigureAwait(false);
            if (entry is not null) return entry.NodeName;
        }

        return await cycles.FindCoreDestinationAsync(originalKey, cancellationToken).ConfigureAwait(false);
    }

    // Biller statis yang tutup: tolak transaksi yang membuka siklus; siklus yang sudah dibuka tetap diteruskan.
    private async Task<RoutingDecision?> CheckStaticScheduleAsync(string routingType, RoutingContext ctx, string? target, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(target) || !health.IsFailoverEnabled(routingType)) return null;

        ScheduleRule? closedBy = schedule.ClosedBy(routingType, ctx.ProductId, target, schedule.Now);
        if (closedBy is not null)
        {
            return await OpensCycleAsync(ctx, cancellationToken).ConfigureAwait(false)
                ? await RejectAsync(routingType, ctx, RouteSelection.Reject(target, closedBy.Id), cancellationToken).ConfigureAwait(false)
                : null;
        }

        await SaveStaticCycleAsync(routingType, ctx, target, cancellationToken).ConfigureAwait(false);
        return null;
    }

    // Static tidak mencatat siklus kecuali biller punya aturan jadwal: inquiry dicatat supaya payment setelah jadwal Tutup tetap diteruskan.
    private async Task SaveStaticCycleAsync(string routingType, RoutingContext ctx, string node, CancellationToken cancellationToken)
    {
        if (ctx.TranType != TranType.Inquiry || !schedule.HasRulesFor(node)) return;
        try
        {
            await SaveCycleAsync(routingType, ctx, node, inquiry: true, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Sticky routing refnum {Refnum} gagal dicatat", ctx.Refnum);
        }
    }

    // INQUIRY selalu membuka siklus; PAYMENT bila belum ada catatan; ADVICE/REVERSAL tidak pernah ditolak.
    private async Task<bool> OpensCycleAsync(RoutingContext ctx, CancellationToken cancellationToken)
    {
        switch (ctx.TranType)
        {
            case TranType.Inquiry:
                return true;
            case TranType.Payment:
                try
                {
                    return await cycles.FindByReferenceAsync(ctx.MerchantId, ctx.TerminalId, ctx.Refnum, cancellationToken).ConfigureAwait(false) is null;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Sticky routing refnum {Refnum} gagal dibaca", ctx.Refnum);
                    return true;
                }

            default:
                return false;
        }
    }

    private async Task<RoutingDecision> RejectAsync(string routingType, RoutingContext ctx, RouteSelection selection, CancellationToken cancellationToken)
    {
        try
        {
            await health.LogScheduleRejectAsync(routingType, ctx.ProductId, routingType == RoutingTypes.Margin ? ctx.Denom : null, ctx.TraceNumber,
                selection.NodeName, RoutingDecision.ScheduleRejectRc, selection.ScheduleId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Log penolakan jadwal refnum {Refnum} gagal", ctx.Refnum);
        }

        return new RoutingDecision { Apply = false, Rejected = true, ScheduleId = selection.ScheduleId, NodeName = selection.NodeName };
    }

    private async Task SaveCycleAsync(string routingType, RoutingContext ctx, string? node, bool inquiry, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(node)) return;
        string key = CurrentSwitchKey(ctx);
        await cycles.InsertAsync(new RoutingCycle
        {
            MerchantId = ctx.MerchantId,
            TerminalId = ctx.TerminalId,
            Refnum = ctx.Refnum,
            RoutingType = routingType,
            InstId = ctx.ProductId,
            Denom = routingType == RoutingTypes.Margin ? checked((int)ctx.Denom) : null,
            NodeName = node,
            InquirySwitchKey = inquiry ? key : null,
            SwitchKey = inquiry ? null : key,
        }, cancellationToken).ConfigureAwait(false);
    }

    private static string CurrentSwitchKey(RoutingContext ctx) => SwitchKeys.Build(ctx.TranType, ctx.TransactionDateTime, ctx.TraceNumber, ctx.TerminalId);
}
