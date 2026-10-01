using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace SyncNetPro.Routing;

/// <summary>
/// Implementasi PostgreSQL (database Core) untuk semua store routing. Query sama dengan <c>DbMgr</c> SDK lama.
/// </summary>
public sealed class PostgresRoutingStore(NpgsqlDataSource dataSource, TimeProvider time, ILogger<PostgresRoutingStore> logger)
    : ISupplierHealthStore, IRoutingScheduleStore, ICommitmentStore, IRoutingCycleStore, IRoutingDataStore
{
    // Panjang kolom node_name/inst_id sw_routes_volume.
    private const int MaxKeyLength = 20;

    private DateTime Now => time.GetLocalNow().DateTime;

    // ------------------------------------------------------------------ data referensi

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductRouteEntry>> GetProductRoutesAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT si.inst_id, si.node_id, sn.node_name, si.fee_sharing, si.lb_weight
            FROM sw_routes_by_inst si
            JOIN sw_nodes sn ON sn.node_id = si.node_id
            """;
        var rows = await QueryAsync<RouteRow>(sql, null, cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(r => new ProductRouteEntry(r.inst_id ?? string.Empty, r.node_id, r.node_name ?? string.Empty, 1, r.fee_sharing, r.lb_weight ?? 0))];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductRouteEntry>> GetAlternateProductRoutesAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT sa.inst_id, sa.node_id, sn.node_name, sa.priority, sa.fee_sharing, sa.lb_weight
            FROM sw_routes_by_inst_alt sa
            JOIN sw_nodes sn ON sn.node_id = sa.node_id
            WHERE sa.status = '1'
            ORDER BY sa.inst_id, sa.priority
            """;
        var rows = await QueryAsync<RouteRow>(sql, null, cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(r => new ProductRouteEntry(r.inst_id ?? string.Empty, r.node_id, r.node_name ?? string.Empty, r.priority ?? 0, r.fee_sharing, r.lb_weight ?? 0))];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MarginRouteEntry>> GetMarginRoutesAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT sd.inst_id, sd.routing_mode, sn.node_name AS static_node_name
            FROM sw_routes_margin sd
            LEFT JOIN sw_nodes sn ON sn.node_id = sd.static_node_id
            """;
        var rows = await QueryAsync<MarginRow>(sql, null, cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(r => new MarginRouteEntry(r.inst_id ?? string.Empty, r.routing_mode, r.static_node_name))];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SupplierPrice>> GetSupplierPricesAsync(CancellationToken cancellationToken)
    {
        const string sql = "SELECT supplier_id, biller_code, denom, harga_beli, harga_jual, margin, status, priority, lb_weight FROM sw_margin_supplier";
        var rows = await QueryAsync<SupplierPriceRow>(sql, null, cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(r => new SupplierPrice(r.supplier_id ?? string.Empty, r.biller_code ?? string.Empty, r.denom ?? 0, r.harga_beli ?? 0,
            r.harga_jual ?? 0, r.margin ?? 0, r.priority, r.lb_weight ?? 0, r.status == "1"))];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MerchantPrice>> GetMerchantPricesAsync(CancellationToken cancellationToken)
    {
        const string sql = "SELECT merchant_id, biller_code, denom, harga_jual FROM sw_margin_merchant";
        var rows = await QueryAsync<MerchantPriceRow>(sql, null, cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(r => new MerchantPrice(r.merchant_id ?? string.Empty, r.biller_code ?? string.Empty, r.denom ?? 0, r.harga_jual ?? 0))];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FeeRule>> GetFeeRulesAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT product_id, merchant_id, submerchant_id, fee_type, routing_mode, static_node_id,
                fixed_fee, fixed_fee_acq, fixed_fee_mer, fixed_fee_iss, fixed_fee_bil, fixed_fee_swt,
                percent_fee, percent_fee_acq, percent_fee_mer, percent_fee_iss, percent_fee_bil, percent_fee_swt
            FROM sw_fees
            """;
        var rows = await QueryAsync<FeeRow>(sql, null, cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(r => new FeeRule
        {
            ProductId = r.product_id ?? string.Empty,
            MerchantId = r.merchant_id,
            SubMerchantId = r.submerchant_id,
            IsFixedFee = r.fee_type == "0",
            RoutingMode = r.routing_mode,
            StaticNodeId = r.static_node_id,
            FixedFeeTotal = r.fixed_fee ?? 0,
            FixedFeeAcquirer = r.fixed_fee_acq ?? 0,
            FixedFeeMerchant = r.fixed_fee_mer ?? 0,
            FixedFeeIssuer = r.fixed_fee_iss ?? 0,
            FixedFeeBiller = r.fixed_fee_bil ?? 0,
            FixedFeeSwitch = r.fixed_fee_swt ?? 0,
            PercentFeeTotal = r.percent_fee ?? 0,
            PercentFeeAcquirer = r.percent_fee_acq ?? 0,
            PercentFeeMerchant = r.percent_fee_mer ?? 0,
            PercentFeeIssuer = r.percent_fee_iss ?? 0,
            PercentFeeBiller = r.percent_fee_bil ?? 0,
            PercentFeeSwitch = r.percent_fee_swt ?? 0,
        })];
    }

    // ------------------------------------------------------------------ health check

    /// <inheritdoc />
    public async Task<IReadOnlyList<SupplierHealth>> GetSupplierHealthAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT supplier_id, status, last_rc_code, consecutive_suspect_count, consecutive_failed_count, consecutive_pending_count,
                consecutive_latency_count, block_reason, last_latency_ms, retry_count, last_tran_dt, blocked_since, blocked_until, updated_by, updated_dt
            FROM sw_routes_supplier_status
            """;
        var rows = await QueryAsync<HealthRow>(sql, null, cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(r => new SupplierHealth
        {
            SupplierId = r.supplier_id ?? string.Empty,
            Status = Enum.TryParse(r.status, ignoreCase: true, out SupplierHealthStatus s) ? s : SupplierHealthStatus.Active,
            LastRcCode = r.last_rc_code,
            ConsecutiveTimeoutCount = r.consecutive_suspect_count ?? 0,
            ConsecutiveFailedCount = r.consecutive_failed_count ?? 0,
            ConsecutivePendingCount = r.consecutive_pending_count ?? 0,
            ConsecutiveLatencyCount = r.consecutive_latency_count ?? 0,
            BlockReason = r.block_reason,
            LastLatencyMs = r.last_latency_ms,
            RetryCount = r.retry_count ?? 0,
            LastTransactionAt = r.last_tran_dt,
            BlockedSince = r.blocked_since,
            BlockedUntil = r.blocked_until,
            UpdatedBy = r.updated_by,
            UpdatedAt = r.updated_dt,
        })];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FailoverConfig>> GetFailoverConfigAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT routing_type, inst_id, rc_link_down, rc_suspect, max_consecutive_suspect, link_down_cooldown_minutes, suspect_cooldown_minutes,
                rc_failed, max_consecutive_failed, failed_cooldown_minutes, rc_pending, max_consecutive_pending, pending_cooldown_minutes,
                latency_threshold_ms, max_consecutive_latency, latency_cooldown_minutes, is_active
            FROM sw_routes_failover_config
            """;
        var rows = await QueryAsync<ConfigRow>(sql, null, cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(r => new FailoverConfig
        {
            RoutingType = r.routing_type ?? string.Empty,
            InstId = r.inst_id,
            RcLinkDown = r.rc_link_down ?? string.Empty,
            LinkDownCooldownMinutes = r.link_down_cooldown_minutes,
            RcTimeout = r.rc_suspect ?? string.Empty,
            MaxConsecutiveTimeout = r.max_consecutive_suspect ?? 0,
            TimeoutCooldownMinutes = r.suspect_cooldown_minutes,
            RcFailed = r.rc_failed,
            MaxConsecutiveFailed = r.max_consecutive_failed ?? 0,
            FailedCooldownMinutes = r.failed_cooldown_minutes,
            RcPending = r.rc_pending,
            MaxConsecutivePending = r.max_consecutive_pending ?? 0,
            PendingCooldownMinutes = r.pending_cooldown_minutes,
            LatencyThresholdMs = r.latency_threshold_ms,
            MaxConsecutiveLatency = r.max_consecutive_latency ?? 0,
            LatencyCooldownMinutes = r.latency_cooldown_minutes,
            IsActive = r.is_active == "1",
        })];
    }

    /// <inheritdoc />
    public Task UpsertSupplierHealthAsync(SupplierHealth health, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(health);
        const string sql = """
            INSERT INTO sw_routes_supplier_status
                (supplier_id, status, last_rc_code, consecutive_suspect_count, consecutive_failed_count, consecutive_pending_count, consecutive_latency_count,
                 block_reason, last_latency_ms, retry_count, last_tran_dt, blocked_since, blocked_until, updated_by, updated_dt)
            VALUES (@supplier_id, @status, @last_rc_code, @consecutive_suspect_count, @consecutive_failed_count, @consecutive_pending_count, @consecutive_latency_count,
                 @block_reason, @last_latency_ms, @retry_count, @last_tran_dt, @blocked_since, @blocked_until, @updated_by, @updated_dt)
            ON CONFLICT (supplier_id) DO UPDATE SET
                status = @status, last_rc_code = @last_rc_code, consecutive_suspect_count = @consecutive_suspect_count,
                consecutive_failed_count = @consecutive_failed_count, consecutive_pending_count = @consecutive_pending_count,
                consecutive_latency_count = @consecutive_latency_count, block_reason = @block_reason, last_latency_ms = @last_latency_ms,
                retry_count = @retry_count, last_tran_dt = @last_tran_dt, blocked_since = @blocked_since, blocked_until = @blocked_until,
                updated_by = @updated_by, updated_dt = @updated_dt
            """;
        return ExecuteAsync(sql, new
        {
            supplier_id = health.SupplierId,
            status = health.Status.ToString().ToUpperInvariant(),
            last_rc_code = health.LastRcCode,
            consecutive_suspect_count = health.ConsecutiveTimeoutCount,
            consecutive_failed_count = health.ConsecutiveFailedCount,
            consecutive_pending_count = health.ConsecutivePendingCount,
            consecutive_latency_count = health.ConsecutiveLatencyCount,
            block_reason = health.BlockReason,
            last_latency_ms = health.LastLatencyMs,
            retry_count = health.RetryCount,
            last_tran_dt = health.LastTransactionAt,
            blocked_since = health.BlockedSince,
            blocked_until = health.BlockedUntil,
            updated_by = health.UpdatedBy,
            updated_dt = health.UpdatedAt ?? Now,
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task InsertFailoverLogAsync(FailoverLogEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        const string sql = """
            INSERT INTO sw_routes_failover_log
                (routing_type, inst_id, denom, trace_number, from_supplier_id, to_supplier_id, rc_code, reason, latency_ms, schedule_id, created_dt)
            VALUES (@routing_type, @inst_id, @denom, @trace_number, @from_supplier_id, @to_supplier_id, @rc_code, @reason, @latency_ms, @schedule_id, @created_dt)
            """;
        return ExecuteAsync(sql, new
        {
            routing_type = entry.RoutingType,
            inst_id = entry.InstId,
            denom = entry.Denom is long d and >= int.MinValue and <= int.MaxValue ? (int?)d : null,
            trace_number = entry.TraceNumber,
            from_supplier_id = entry.FromSupplierId,
            to_supplier_id = entry.ToSupplierId,
            rc_code = entry.RcCode,
            reason = entry.Reason,
            latency_ms = entry.LatencyMs,
            schedule_id = entry.ScheduleId,
            created_dt = entry.CreatedAt,
        }, cancellationToken);
    }

    // ------------------------------------------------------------------ siklus (sticky)

    /// <inheritdoc />
    public async Task<RoutingCycle?> FindByReferenceAsync(string? merchantId, string? terminalId, string? refnum, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, merchant_id, terminal_id, refnum, routing_type, inst_id, denom, node_name, inquiry_switch_key, switch_key
            FROM sw_routes_tran_map
            WHERE merchant_id = @merchant_id AND terminal_id = @terminal_id AND refnum = @refnum
            ORDER BY id DESC LIMIT 1
            """;
        var rows = await QueryAsync<CycleRow>(sql, new { merchant_id = merchantId, terminal_id = terminalId, refnum }, cancellationToken).ConfigureAwait(false);
        return rows.Select(ToCycle).FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<RoutingCycle?> FindBySwitchKeyAsync(string switchKey, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, merchant_id, terminal_id, refnum, routing_type, inst_id, denom, node_name, inquiry_switch_key, switch_key
            FROM sw_routes_tran_map WHERE switch_key = @switch_key ORDER BY id DESC LIMIT 1
            """;
        var rows = await QueryAsync<CycleRow>(sql, new { switch_key = switchKey }, cancellationToken).ConfigureAwait(false);
        return rows.Select(ToCycle).FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<long> InsertAsync(RoutingCycle cycle, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cycle);
        const string sql = """
            INSERT INTO sw_routes_tran_map
                (merchant_id, terminal_id, refnum, routing_type, inst_id, denom, node_name, inquiry_switch_key, switch_key, created_dt)
            VALUES (@merchant_id, @terminal_id, @refnum, @routing_type, @inst_id, @denom, @node_name, @inquiry_switch_key, @switch_key, @created_dt)
            RETURNING id
            """;
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await connection.ExecuteScalarAsync<long>(new CommandDefinition(sql, new
        {
            merchant_id = cycle.MerchantId,
            terminal_id = cycle.TerminalId,
            refnum = cycle.Refnum,
            routing_type = cycle.RoutingType,
            inst_id = cycle.InstId,
            denom = cycle.Denom,
            node_name = cycle.NodeName,
            inquiry_switch_key = cycle.InquirySwitchKey,
            switch_key = cycle.SwitchKey,
            created_dt = Now,
        }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task UpdateSwitchKeyAsync(long id, string switchKey, CancellationToken cancellationToken) =>
        ExecuteAsync("UPDATE sw_routes_tran_map SET switch_key = @switch_key, updated_dt = @updated_dt WHERE id = @id",
            new { id, switch_key = switchKey, updated_dt = Now }, cancellationToken);

    /// <inheritdoc />
    public async Task<string?> FindCoreDestinationAsync(string switchKey, CancellationToken cancellationToken)
    {
        var rows = await QueryAsync<string?>("SELECT dest_node FROM sw_trans_pg WHERE switch_key = @switch_key LIMIT 1", new { switch_key = switchKey }, cancellationToken).ConfigureAwait(false);
        return rows.FirstOrDefault();
    }

    // ------------------------------------------------------------------ jadwal

    /// <inheritdoc />
    public async Task<IReadOnlyList<ScheduleRule>> GetScheduleRulesAsync(DateTime now, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT s.id, s.rule_name, s.rule_type, s.routing_type, s.inst_id, sn.node_name, s.priority, s.recurrence, s.start_dt, s.end_dt,
                s.time_start, s.time_end, s.days_of_week, s.days_of_month, s.valid_from, s.valid_until
            FROM sw_routes_schedule s
            JOIN sw_nodes sn ON sn.node_id = s.node_id
            WHERE s.status = '1'
              AND (s.recurrence <> 'ONCE' OR s.end_dt > @now)
              AND (s.valid_until IS NULL OR s.valid_until >= @today)
            """;
        var rows = await QueryAsync<ScheduleRow>(sql, new { now, today = now.Date }, cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(r => new ScheduleRule
        {
            Id = r.id,
            Name = r.rule_name,
            RuleType = r.rule_type ?? string.Empty,
            RoutingType = r.routing_type,
            InstId = r.inst_id,
            NodeName = r.node_name ?? string.Empty,
            Priority = r.priority,
            Recurrence = r.recurrence ?? string.Empty,
            StartAt = r.start_dt,
            EndAt = r.end_dt,
            TimeStart = r.time_start,
            TimeEnd = r.time_end,
            Days = ParseDays(r.recurrence == ScheduleRecurrences.Weekly ? r.days_of_week : r.days_of_month),
            ValidFrom = r.valid_from?.Date,
            ValidUntil = r.valid_until?.Date,
        })];
    }

    // ------------------------------------------------------------------ Volume & Tiering

    /// <inheritdoc />
    public async Task<bool> IsCommitmentActiveAsync(CancellationToken cancellationToken)
    {
        var rows = await QueryAsync<string?>("SELECT is_active FROM sw_routes_commitment_config WHERE id = 1", null, cancellationToken).ConfigureAwait(false);
        return rows.FirstOrDefault() == "1";
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CommitmentRule>> GetCommitmentRulesAsync(DateTime today, CancellationToken cancellationToken)
    {
        // Aturan yang berakhir sejak awal bulan lalu ikut dimuat untuk pemeriksaan target terlewat.
        const string sql = """
            SELECT c.id, c.rule_name, c.rule_type, c.routing_type, sn.node_name, c.inst_id, c.metric, c.period_type,
                c.threshold_value, c.warn_pct, c.valid_from, c.valid_until, c.created_dt
            FROM sw_routes_commitment c
            JOIN sw_nodes sn ON sn.node_id = c.node_id
            WHERE c.status = '1' AND (c.valid_until IS NULL OR c.valid_until >= @from::date)
            """;
        var rows = await QueryAsync<CommitmentRow>(sql, new { from = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Unspecified).AddMonths(-1) }, cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(r => new CommitmentRule
        {
            Id = r.id,
            Name = r.rule_name,
            RuleType = r.rule_type ?? string.Empty,
            RoutingType = r.routing_type ?? string.Empty,
            NodeName = r.node_name ?? string.Empty,
            InstId = r.inst_id,
            Metric = r.metric ?? string.Empty,
            PeriodType = r.period_type ?? string.Empty,
            Threshold = r.threshold_value,
            WarnPct = r.warn_pct,
            ValidFrom = r.valid_from?.Date,
            ValidUntil = r.valid_until?.Date,
            CreatedAt = r.created_dt,
        })];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VolumeTotal>> GetVolumeTotalsAsync(DateTime today, IReadOnlyCollection<string> nodeNames, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(nodeNames);
        if (nodeNames.Count == 0) return [];
        const string sql = """
            SELECT routing_type, node_name, inst_id,
                COALESCE(SUM(tran_count)  FILTER (WHERE volume_date = @today::date), 0)     AS today_count,
                COALESCE(SUM(tran_amount) FILTER (WHERE volume_date = @today::date), 0)     AS today_amount,
                COALESCE(SUM(tran_count)  FILTER (WHERE volume_date = @yesterday::date), 0) AS yesterday_count,
                COALESCE(SUM(tran_amount) FILTER (WHERE volume_date = @yesterday::date), 0) AS yesterday_amount,
                COALESCE(SUM(tran_count)  FILTER (WHERE volume_date >= @month_start::date AND volume_date <= @today::date), 0) AS month_count,
                COALESCE(SUM(tran_amount) FILTER (WHERE volume_date >= @month_start::date AND volume_date <= @today::date), 0) AS month_amount,
                COALESCE(SUM(tran_count)  FILTER (WHERE volume_date < @month_start::date), 0) AS prev_month_count,
                COALESCE(SUM(tran_amount) FILTER (WHERE volume_date < @month_start::date), 0) AS prev_month_amount
            FROM sw_routes_volume
            WHERE volume_date >= @prev_month_start::date AND volume_date <= @today::date AND node_name = ANY(@nodes)
            GROUP BY routing_type, node_name, inst_id
            """;
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var rows = await QueryAsync<VolumeRow>(sql, new
        {
            today = today.Date,
            yesterday = today.Date.AddDays(-1),
            month_start = monthStart,
            prev_month_start = monthStart.AddMonths(-1),
            nodes = nodeNames.ToArray(),
        }, cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(r => new VolumeTotal
        {
            RoutingType = r.routing_type ?? string.Empty,
            NodeName = r.node_name ?? string.Empty,
            InstId = r.inst_id ?? string.Empty,
            TodayCount = r.today_count,
            TodayAmount = r.today_amount,
            YesterdayCount = r.yesterday_count,
            YesterdayAmount = r.yesterday_amount,
            MonthCount = r.month_count,
            MonthAmount = r.month_amount,
            PreviousMonthCount = r.prev_month_count,
            PreviousMonthAmount = r.prev_month_amount,
        })];
    }

    /// <inheritdoc />
    public async Task AddVolumesAsync(IReadOnlyList<VolumeDelta> deltas, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(deltas);
        const string sql = """
            INSERT INTO sw_routes_volume (volume_date, routing_type, node_name, inst_id, tran_count, tran_amount, updated_dt)
            VALUES (@volume_date::date, @routing_type, @node_name, @inst_id, @tran_count, @tran_amount, @updated_dt)
            ON CONFLICT (volume_date, routing_type, node_name, inst_id) DO UPDATE SET
                tran_count = sw_routes_volume.tran_count + EXCLUDED.tran_count,
                tran_amount = sw_routes_volume.tran_amount + EXCLUDED.tran_amount,
                updated_dt = EXCLUDED.updated_dt
            """;

        // SDK lama membuang delta yang tidak muat kolom tanpa jejak; di sini tetap dibuang (tidak dapat disimpan) tetapi dicatat.
        var valid = deltas.Where(d => d.NodeName.Length <= MaxKeyLength && d.InstId.Length <= MaxKeyLength).ToList();
        foreach (VolumeDelta skipped in deltas.Except(valid))
        {
            logger.LogWarning("Volume {Node}/{Product} tidak disimpan: nama melebihi {Max} karakter", skipped.NodeName, skipped.InstId, MaxKeyLength);
        }

        if (valid.Count == 0) return;

        DateTime now = Now;
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        foreach (VolumeDelta d in valid)
        {
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                volume_date = d.Date.Date,
                routing_type = d.RoutingType,
                node_name = d.NodeName,
                inst_id = d.InstId,
                tran_count = d.Count,
                tran_amount = d.Amount,
                updated_dt = now,
            }, transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task InsertCommitmentEventsAsync(IReadOnlyList<CommitmentEvent> events, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(events);
        const string sql = """
            INSERT INTO sw_routes_commitment_log
                (commitment_id, rule_type, routing_type, node_name, inst_id, metric, period_type, period_start, event, volume_value, threshold_value, created_dt)
            VALUES (@commitment_id, @rule_type, @routing_type, @node_name, @inst_id, @metric, @period_type, @period_start::date, @event,
                @volume_value, @threshold_value, @created_dt)
            ON CONFLICT ON CONSTRAINT uq_sw_routes_commitment_log_event DO NOTHING
            """;
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        foreach (CommitmentEvent e in events)
        {
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                commitment_id = e.CommitmentId,
                rule_type = e.RuleType,
                routing_type = e.RoutingType,
                node_name = e.NodeName,
                inst_id = e.InstId,
                metric = e.Metric,
                period_type = e.PeriodType,
                period_start = e.PeriodStart.Date,
                @event = e.Event,
                volume_value = e.Volume,
                threshold_value = e.Threshold,
                created_dt = e.CreatedAt,
            }, transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    // ------------------------------------------------------------------ helper

    private async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await connection.QueryAsync<T>(new CommandDefinition(sql, param, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private async Task ExecuteAsync(string sql, object param, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await connection.ExecuteAsync(new CommandDefinition(sql, param, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private static List<int> ParseDays(string? value) =>
        string.IsNullOrEmpty(value) ? [] : [.. value.Split(',').Select(s => int.TryParse(s.Trim(), out int d) ? d : 0).Where(d => d > 0)];

    private static RoutingCycle ToCycle(CycleRow r) => new()
    {
        Id = r.id,
        MerchantId = r.merchant_id,
        TerminalId = r.terminal_id,
        Refnum = r.refnum,
        RoutingType = r.routing_type ?? string.Empty,
        InstId = r.inst_id,
        Denom = r.denom,
        NodeName = r.node_name ?? string.Empty,
        InquirySwitchKey = r.inquiry_switch_key,
        SwitchKey = r.switch_key,
    };

#pragma warning disable CA1812, IDE1006, SA1300 // baris hasil query (Dapper), nama = kolom
    private sealed class RouteRow
    {
        public string? inst_id { get; set; }

        public int node_id { get; set; }

        public string? node_name { get; set; }

        public short? priority { get; set; }

        public int? fee_sharing { get; set; }

        public int? lb_weight { get; set; }
    }

    private sealed class MarginRow
    {
        public string? inst_id { get; set; }

        public string? routing_mode { get; set; }

        public string? static_node_name { get; set; }
    }

    private sealed class SupplierPriceRow
    {
        public string? supplier_id { get; set; }

        public string? biller_code { get; set; }

        public int? denom { get; set; }

        public int? harga_beli { get; set; }

        public int? harga_jual { get; set; }

        public int? margin { get; set; }

        public string? status { get; set; }

        public int? priority { get; set; }

        public int? lb_weight { get; set; }
    }

    private sealed class MerchantPriceRow
    {
        public string? merchant_id { get; set; }

        public string? biller_code { get; set; }

        public int? denom { get; set; }

        public int? harga_jual { get; set; }
    }

    private sealed class FeeRow
    {
        public string? product_id { get; set; }

        public string? merchant_id { get; set; }

        public string? submerchant_id { get; set; }

        public string? fee_type { get; set; }

        public string? routing_mode { get; set; }

        public int? static_node_id { get; set; }

        public int? fixed_fee { get; set; }

        public int? fixed_fee_acq { get; set; }

        public int? fixed_fee_mer { get; set; }

        public int? fixed_fee_iss { get; set; }

        public int? fixed_fee_bil { get; set; }

        public int? fixed_fee_swt { get; set; }

        public decimal? percent_fee { get; set; }

        public decimal? percent_fee_acq { get; set; }

        public decimal? percent_fee_mer { get; set; }

        public decimal? percent_fee_iss { get; set; }

        public decimal? percent_fee_bil { get; set; }

        public decimal? percent_fee_swt { get; set; }
    }

    private sealed class HealthRow
    {
        public string? supplier_id { get; set; }

        public string? status { get; set; }

        public string? last_rc_code { get; set; }

        public int? consecutive_suspect_count { get; set; }

        public int? consecutive_failed_count { get; set; }

        public int? consecutive_pending_count { get; set; }

        public int? consecutive_latency_count { get; set; }

        public string? block_reason { get; set; }

        public int? last_latency_ms { get; set; }

        public int? retry_count { get; set; }

        public DateTime? last_tran_dt { get; set; }

        public DateTime? blocked_since { get; set; }

        public DateTime? blocked_until { get; set; }

        public string? updated_by { get; set; }

        public DateTime? updated_dt { get; set; }
    }

    private sealed class ConfigRow
    {
        public string? routing_type { get; set; }

        public string? inst_id { get; set; }

        public string? rc_link_down { get; set; }

        public string? rc_suspect { get; set; }

        public int? max_consecutive_suspect { get; set; }

        public int? link_down_cooldown_minutes { get; set; }

        public int? suspect_cooldown_minutes { get; set; }

        public string? rc_failed { get; set; }

        public int? max_consecutive_failed { get; set; }

        public int? failed_cooldown_minutes { get; set; }

        public string? rc_pending { get; set; }

        public int? max_consecutive_pending { get; set; }

        public int? pending_cooldown_minutes { get; set; }

        public int? latency_threshold_ms { get; set; }

        public int? max_consecutive_latency { get; set; }

        public int? latency_cooldown_minutes { get; set; }

        public string? is_active { get; set; }
    }

    private sealed class CycleRow
    {
        public long id { get; set; }

        public string? merchant_id { get; set; }

        public string? terminal_id { get; set; }

        public string? refnum { get; set; }

        public string? routing_type { get; set; }

        public string? inst_id { get; set; }

        public int? denom { get; set; }

        public string? node_name { get; set; }

        public string? inquiry_switch_key { get; set; }

        public string? switch_key { get; set; }
    }

    private sealed class ScheduleRow
    {
        public int id { get; set; }

        public string? rule_name { get; set; }

        public string? rule_type { get; set; }

        public string? routing_type { get; set; }

        public string? inst_id { get; set; }

        public string? node_name { get; set; }

        public short? priority { get; set; }

        public string? recurrence { get; set; }

        public DateTime? start_dt { get; set; }

        public DateTime? end_dt { get; set; }

        public TimeSpan? time_start { get; set; }

        public TimeSpan? time_end { get; set; }

        public string? days_of_week { get; set; }

        public string? days_of_month { get; set; }

        public DateTime? valid_from { get; set; }

        public DateTime? valid_until { get; set; }
    }

    private sealed class CommitmentRow
    {
        public int id { get; set; }

        public string? rule_name { get; set; }

        public string? rule_type { get; set; }

        public string? routing_type { get; set; }

        public string? node_name { get; set; }

        public string? inst_id { get; set; }

        public string? metric { get; set; }

        public string? period_type { get; set; }

        public decimal threshold_value { get; set; }

        public short? warn_pct { get; set; }

        public DateTime? valid_from { get; set; }

        public DateTime? valid_until { get; set; }

        public DateTime? created_dt { get; set; }
    }

    private sealed class VolumeRow
    {
        public string? routing_type { get; set; }

        public string? node_name { get; set; }

        public string? inst_id { get; set; }

        public long today_count { get; set; }

        public decimal today_amount { get; set; }

        public long yesterday_count { get; set; }

        public decimal yesterday_amount { get; set; }

        public long month_count { get; set; }

        public decimal month_amount { get; set; }

        public long prev_month_count { get; set; }

        public decimal prev_month_amount { get; set; }
    }
#pragma warning restore CA1812, IDE1006, SA1300
}
