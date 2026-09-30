using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Logging;
using SyncNetPro.Contracts;

namespace SyncNetPro.Routing;

/// <summary>
/// Volume &amp; Tiering (Level 3) — port 1:1 <c>CommitmentRepository</c> SDK lama.
/// <para>
/// Volume (tanggal = tanggal PAYMENT asal): PAYMENT sukses +1; ADVICE sukses +1 bila PAYMENT asal belum dihitung;
/// REVERSAL sukses −1 bila PAYMENT asal dihitung instance ini. Delta disimpan di memori lalu ditulis ke
/// <c>sw_routes_volume</c> setiap <c>flushInterval</c> (UPSERT atomik, aman banyak instance); total dimuat ulang per aturan.
/// </para>
/// Kuota (LIMIT): biller dibuang bila volume ≥ nilai (semua habis = kuota diabaikan). Target Tier (TARGET): biller
/// didahulukan selama volume &lt; nilai. Event ditulis sekali per aturan + periode + event + nilai.
/// </summary>
public sealed class CommitmentTracker : IAsyncDisposable
{
    private static readonly TimeSpan CountedRetention = TimeSpan.FromDays(2);

    private readonly ICommitmentStore _store;
    private readonly TimeProvider _time;
    private readonly TimeSpan _flushInterval;
    private readonly ILogger _logger;

    private readonly Lock _sync = new();
    private readonly Dictionary<string, CountedPayment> _counted = [];
    private readonly HashSet<string> _loggedKeys = [];
    private readonly ConcurrentQueue<CommitmentEvent> _events = new();
    private readonly SemaphoreSlim _flushLock = new(1, 1);

    private volatile bool _active;
    private volatile IReadOnlyList<CommitmentRule> _rules = [];
    private volatile HashSet<string> _nodes = [];
    private volatile RuleTotals _totals = RuleTotals.Empty;
    private Dictionary<VolumeKey, Delta> _pending = [];
    private List<VolumeDelta> _inFlight = [];
    private DateTime? _lastMissedCheck;
    private ITimer? _timer;

    /// <summary>Membuat tracker.</summary>
    /// <param name="store">Penyimpanan.</param>
    /// <param name="time">Jam.</param>
    /// <param name="flushInterval">Interval flush; <see cref="TimeSpan.Zero"/> = tanpa timer (test memanggil <see cref="FlushAsync"/>).</param>
    /// <param name="logger">Logger.</param>
    public CommitmentTracker(ICommitmentStore store, TimeProvider time, TimeSpan flushInterval, ILogger logger)
    {
        _store = store;
        _time = time;
        _flushInterval = flushInterval;
        _logger = logger;
    }

    /// <summary>Fitur ON.</summary>
    public bool IsActive => _active;

    /// <summary>ON dan ada aturan.</summary>
    public bool HasRules => _active && _rules.Count > 0;

    private DateTime Now => _time.GetLocalNow().DateTime;

    /// <summary>Muat ON/OFF, aturan, dan total (start/RESYNC). Delta yang belum tersimpan ditulis dulu. Gagal = OFF.</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _flushLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            try
            {
                await FlushCoreAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Volume & Tiering flush gagal");
            }

            bool active = false;
            IReadOnlyList<CommitmentRule> rules = [];
            try
            {
                active = await _store.IsCommitmentActiveAsync(cancellationToken).ConfigureAwait(false);
                if (active) rules = await _store.GetCommitmentRulesAsync(Now.Date, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                active = false;
                _logger.LogWarning(ex, "Volume & Tiering tidak dapat dimuat; fitur dianggap OFF");
            }

            _rules = rules;
            _nodes = [.. rules.Where(r => !string.IsNullOrEmpty(r.NodeName)).Select(r => r.NodeName)];
            _active = active;

            if (!active)
            {
                lock (_sync)
                {
                    _pending = [];
                    _inFlight = [];
                    _counted.Clear();
                }

                _totals = RuleTotals.Empty;
            }
            else
            {
                try
                {
                    await ReloadTotalsAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Volume & Tiering gagal memuat volume");
                }
            }
        }
        finally
        {
            _flushLock.Release();
        }

        // Timer hanya berjalan saat ON (OFF tanpa beban).
        if (_active && _timer is null && _flushInterval > TimeSpan.Zero)
        {
            _timer = _time.CreateTimer(_ => _ = FlushSafeAsync(), null, _flushInterval, _flushInterval);
        }
        else if (!_active && _timer is not null)
        {
            await _timer.DisposeAsync().ConfigureAwait(false);
            _timer = null;
        }
    }

    /// <summary>
    /// Respons dari biller: hanya PAYMENT, ADVICE, REVERSAL sukses yang berpengaruh.
    /// </summary>
    /// <param name="routingType">Kategori.</param>
    /// <param name="nodeName">Biller.</param>
    /// <param name="productId">Produk.</param>
    /// <param name="tranType">Tran type.</param>
    /// <param name="rcCode">RC.</param>
    /// <param name="amount">Nominal.</param>
    /// <param name="switchKey">Kunci PAYMENT ini.</param>
    /// <param name="originalSwitchKey">Kunci PAYMENT asal (ADVICE/REVERSAL).</param>
    /// <param name="transactionDate">Tanggal PAYMENT (asal); <c>null</c> = hari ini.</param>
    public void RecordVolume(string routingType, string? nodeName, string? productId, string? tranType, string? rcCode, decimal amount,
        string? switchKey = null, string? originalSwitchKey = null, DateTime? transactionDate = null)
    {
        if (!_active || string.IsNullOrEmpty(nodeName) || string.IsNullOrEmpty(productId)) return;
        if (!SupplierHealthTracker.IsSuccessCode(rcCode)) return;

        DateTime now = Now;
        var key = new VolumeKey(SaneDate(transactionDate, now), routingType, nodeName, productId);

        lock (_sync)
        {
            switch (tranType)
            {
                case TranType.Payment:
                    if (!Count(switchKey, key, amount, now)) return;
                    break;

                case TranType.Advice:
                    // Cek status PAYMENT suspect: dihitung sekali dari PAYMENT asal.
                    if (string.IsNullOrEmpty(originalSwitchKey) || !Count(originalSwitchKey, key, amount, now)) return;
                    break;

                case TranType.Reversal:
                    // PAYMENT yang tidak dihitung di sini tidak dikurangi; selisih dibetulkan rekonsiliasi harian.
                    if (string.IsNullOrEmpty(originalSwitchKey) || !_counted.Remove(originalSwitchKey, out CountedPayment? paid)) return;
                    AddPending(paid.Key, -1, -paid.Amount);
                    return;

                default:
                    return;
            }
        }

        if (_nodes.Contains(nodeName)) CheckThresholds(_rules.Where(r => r.InScope(routingType, nodeName, productId)), now);
    }

    /// <summary>Volume aturan pada periode yang memuat <paramref name="day"/>: total DB tercache + delta lokal.</summary>
    public decimal Volume(CommitmentRule rule, DateTime day)
    {
        ArgumentNullException.ThrowIfNull(rule);
        DateTime from = rule.PeriodStart(day);
        DateTime to = day.Date;
        decimal sum = StoredTotal(rule, from);
        lock (_sync)
        {
            sum += Sum(_pending.Select(p => p.Key.ToDelta(p.Value)), rule, from, to);
            sum += Sum(_inFlight, rule, from, to);
        }

        return sum;
    }

    /// <summary>
    /// Terapkan kuota &amp; target pada kandidat hasil Jadwal Routing: buang biller yang kuotanya habis (semua habis = kuota
    /// diabaikan), lalu biller yang mengejar target dipindah ke depan setelah Prioritas Jadwal (stabil).
    /// </summary>
    public CommitmentOrder<T> Apply<T>(IReadOnlyList<T> ordered, Func<T, string> nodeName, Func<T, bool> isScheduled, string routingType, string? productId)
    {
        ArgumentNullException.ThrowIfNull(ordered);
        ArgumentNullException.ThrowIfNull(nodeName);
        ArgumentNullException.ThrowIfNull(isScheduled);
        List<T> list = [.. ordered];
        if (!_active || list.Count == 0) return new CommitmentOrder<T>(list, new HashSet<string>(), false);

        DateTime now = Now;
        var relevant = _rules.Where(r => r.RoutingType == routingType && (r.InstId is null || r.InstId == productId) && r.IsValidOn(now)).ToList();
        if (relevant.Count == 0) return new CommitmentOrder<T>(list, new HashSet<string>(), false);

        var limitedBy = new Dictionary<string, List<(CommitmentRule Rule, decimal Volume)>>();
        var targeted = new HashSet<string>();
        foreach (string node in list.Select(nodeName).Distinct())
        {
            foreach (CommitmentRule r in relevant.Where(r => r.NodeName == node))
            {
                decimal v = Volume(r, now);
                if (r.RuleType == CommitmentConstants.Limit && v >= r.Threshold)
                {
                    if (!limitedBy.TryGetValue(node, out List<(CommitmentRule, decimal)>? by)) limitedBy[node] = by = [];
                    by.Add((r, v));
                }
                else if (r.RuleType == CommitmentConstants.Target && v < r.Threshold)
                {
                    targeted.Add(node);
                }
            }
        }

        List<T> open = [.. list.Where(x => !limitedBy.ContainsKey(nodeName(x)))];
        bool allLimited = open.Count == 0;
        if (allLimited)
        {
            // Semua kandidat habis kuota: kuota diabaikan, transaksi tidak ditolak.
            open = list;
            foreach ((CommitmentRule r, decimal v) in limitedBy.Values.SelectMany(x => x))
            {
                QueueEvent(r, CommitmentConstants.LimitIgnored, v, r.PeriodStart(now), productId, now);
            }
        }

        List<T> result = [.. open
            .Select((x, i) => (Item: x, Rank: isScheduled(x) ? 0 : targeted.Contains(nodeName(x)) ? 1 : 2, Index: i))
            .OrderBy(x => x.Rank)
            .ThenBy(x => x.Index)
            .Select(x => x.Item)];
        return new CommitmentOrder<T>(result, targeted, allLimited);
    }

    /// <summary>Tulis delta, muat ulang total, periksa event, tulis log. Flush yang sedang berjalan tidak ditunggu.</summary>
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        if (!_active || !await _flushLock.WaitAsync(0, cancellationToken).ConfigureAwait(false)) return;
        try
        {
            await FlushCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _flushLock.Release();
        }
    }

    /// <summary>Menunggu flush yang sedang berjalan lalu flush terakhir (dipanggil saat berhenti).</summary>
    public async Task FlushFinalAsync(CancellationToken cancellationToken = default)
    {
        if (!_active) return;
        await _flushLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await FlushCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _flushLock.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_timer is not null) await _timer.DisposeAsync().ConfigureAwait(false);
        _flushLock.Dispose();
    }

    private bool Count(string? key, VolumeKey volumeKey, decimal amount, DateTime now)
    {
        if (!string.IsNullOrEmpty(key))
        {
            if (_counted.ContainsKey(key)) return false;
            _counted[key] = new CountedPayment(volumeKey, amount, now);
        }

        AddPending(volumeKey, 1, amount);
        return true;
    }

    // Jam CA yang meleset > 2 hari diabaikan.
    private static DateTime SaneDate(DateTime? date, DateTime now) =>
        date is DateTime d && Math.Abs((d - now).TotalDays) <= 2 ? d.Date : now.Date;

    private void AddPending(VolumeKey key, long count, decimal amount)
    {
        if (!_pending.TryGetValue(key, out Delta? v)) _pending[key] = v = new Delta();
        v.Count += count;
        v.Amount += amount;
    }

    private decimal StoredTotal(CommitmentRule rule, DateTime periodStart)
    {
        RuleTotals totals = _totals;
        if (totals.LoadedAt == DateTime.MinValue) return 0;
        DateTime current = rule.PeriodStart(totals.LoadedAt);
        if (periodStart == current) return totals.Current.GetValueOrDefault(rule.Id);
        if (periodStart == rule.PeriodStart(current.AddDays(-1))) return totals.Previous.GetValueOrDefault(rule.Id);
        return 0;
    }

    private static decimal Sum(IEnumerable<VolumeDelta> rows, CommitmentRule rule, DateTime from, DateTime to)
    {
        decimal sum = 0;
        foreach (VolumeDelta v in rows)
        {
            if (v.NodeName != rule.NodeName || v.RoutingType != rule.RoutingType) continue;
            if (rule.InstId is not null && v.InstId != rule.InstId) continue;
            if (v.Date < from || v.Date > to) continue;
            sum += rule.Metric == CommitmentConstants.Count ? v.Count : v.Amount;
        }

        return sum;
    }

    private void CheckThresholds(IEnumerable<CommitmentRule> rules, DateTime now)
    {
        foreach (CommitmentRule r in rules)
        {
            if (!r.IsValidOn(now)) continue;
            decimal v = Volume(r, now);
            DateTime period = r.PeriodStart(now);
            if (r.RuleType == CommitmentConstants.Limit)
            {
                decimal warnAt = r.Threshold * (r.WarnPct ?? 80) / 100m;
                if (v >= warnAt) QueueEvent(r, CommitmentConstants.LimitWarn, v, period, r.InstId, now);
                if (v >= r.Threshold) QueueEvent(r, CommitmentConstants.LimitReached, v, period, r.InstId, now);
            }
            else if (v >= r.Threshold)
            {
                QueueEvent(r, CommitmentConstants.TargetReached, v, period, r.InstId, now);
            }
        }
    }

    // Target periode lalu yang tidak tercapai, sekali per hari (flush pertama hari itu).
    private void CheckMissedTargets(DateTime now)
    {
        if (_lastMissedCheck == now.Date) return;
        _lastMissedCheck = now.Date;
        lock (_sync) _loggedKeys.Clear();

        foreach (CommitmentRule r in _rules.Where(r => r.RuleType == CommitmentConstants.Target))
        {
            DateTime current = r.PeriodStart(now);
            DateTime lastDay = current.AddDays(-1);
            if (!r.IsValidOn(lastDay)) continue;
            if (r.CreatedAt is DateTime created && created >= current) continue;
            decimal v = Volume(r, lastDay);
            if (v < r.Threshold) QueueEvent(r, CommitmentConstants.TargetMissed, v, r.PeriodStart(lastDay), r.InstId, now);
        }
    }

    private void QueueEvent(CommitmentRule r, string evt, decimal volume, DateTime periodStart, string? instId, DateTime now)
    {
        string key = string.Create(CultureInfo.InvariantCulture, $"{r.Id}|{periodStart:yyyyMMdd}|{evt}|{r.Threshold}");
        lock (_sync)
        {
            if (!_loggedKeys.Add(key)) return;
        }

        _events.Enqueue(new CommitmentEvent
        {
            CommitmentId = r.Id,
            RuleType = r.RuleType,
            RoutingType = r.RoutingType,
            NodeName = r.NodeName,
            InstId = instId,
            Metric = r.Metric,
            PeriodType = r.PeriodType,
            PeriodStart = periodStart,
            Event = evt,
            Volume = volume,
            Threshold = r.Threshold,
            CreatedAt = now,
        });
    }

    private async Task FlushSafeAsync()
    {
        try
        {
            await FlushAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Volume & Tiering flush gagal; dicoba lagi pada flush berikutnya");
        }
    }

    // Dipanggil dengan _flushLock dipegang.
    private async Task FlushCoreAsync(CancellationToken cancellationToken)
    {
        DateTime now = Now;
        List<VolumeDelta> deltas;
        List<VolumeDelta> unreloaded;
        lock (_sync)
        {
            deltas = [.. _pending.Where(p => p.Value.Count != 0 || p.Value.Amount != 0).Select(p => p.Key.ToDelta(p.Value))];
            _pending = [];
            // Delta yang sudah ditulis tetapi belum masuk cache (reload sebelumnya gagal) tetap dihitung.
            unreloaded = _inFlight;
            _inFlight = [.. unreloaded, .. deltas];
            foreach (string k in _counted.Where(c => now - c.Value.At > CountedRetention).Select(c => c.Key).ToList()) _counted.Remove(k);
        }

        if (deltas.Count > 0)
        {
            try
            {
                await _store.AddVolumesAsync(deltas, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Gagal simpan: delta dikembalikan dan dicoba lagi pada flush berikutnya.
                lock (_sync)
                {
                    foreach (VolumeDelta d in deltas) AddPending(new VolumeKey(d.Date, d.RoutingType, d.NodeName, d.InstId), d.Count, d.Amount);
                    _inFlight = unreloaded;
                }

                throw;
            }
        }

        if (!_active)
        {
            lock (_sync) _inFlight = [];
            return;
        }

        await ReloadTotalsAsync(cancellationToken).ConfigureAwait(false);

        if (_rules.Count > 0)
        {
            CheckMissedTargets(now);
            CheckThresholds(_rules, now);
        }

        await WriteEventsAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task ReloadTotalsAsync(CancellationToken cancellationToken)
    {
        HashSet<string> nodes = _nodes;
        IReadOnlyList<CommitmentRule> rules = _rules;
        DateTime today = Now.Date;
        var current = new Dictionary<int, decimal>();
        var previous = new Dictionary<int, decimal>();

        if (nodes.Count > 0 && rules.Count > 0)
        {
            IReadOnlyList<VolumeTotal> totals = await _store.GetVolumeTotalsAsync(today, nodes, cancellationToken).ConfigureAwait(false);
            foreach (CommitmentRule r in rules)
            {
                decimal cur = 0, prev = 0;
                foreach (VolumeTotal t in totals)
                {
                    if (t.NodeName != r.NodeName || t.RoutingType != r.RoutingType) continue;
                    if (r.InstId is not null && t.InstId != r.InstId) continue;
                    bool count = r.Metric == CommitmentConstants.Count;
                    if (r.PeriodType == CommitmentConstants.Monthly)
                    {
                        cur += count ? t.MonthCount : t.MonthAmount;
                        prev += count ? t.PreviousMonthCount : t.PreviousMonthAmount;
                    }
                    else
                    {
                        cur += count ? t.TodayCount : t.TodayAmount;
                        prev += count ? t.YesterdayCount : t.YesterdayAmount;
                    }
                }

                current[r.Id] = cur;
                previous[r.Id] = prev;
            }
        }

        lock (_sync)
        {
            _totals = new RuleTotals(today, current, previous);
            _inFlight = [];
        }
    }

    private async Task WriteEventsAsync(CancellationToken cancellationToken)
    {
        var events = new List<CommitmentEvent>();
        while (_events.TryDequeue(out CommitmentEvent? e)) events.Add(e);
        if (events.Count == 0) return;
        try
        {
            await _store.InsertCommitmentEventsAsync(events, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Volume & Tiering gagal menulis {Count} event", events.Count);
        }
    }

    private sealed class Delta
    {
        public long Count { get; set; }

        public decimal Amount { get; set; }
    }

    private readonly record struct VolumeKey(DateTime Date, string RoutingType, string NodeName, string InstId)
    {
        public VolumeDelta ToDelta(Delta d) => new(Date, RoutingType, NodeName, InstId, d.Count, d.Amount);
    }

    private sealed record CountedPayment(VolumeKey Key, decimal Amount, DateTime At);

    private sealed record RuleTotals(DateTime LoadedAt, Dictionary<int, decimal> Current, Dictionary<int, decimal> Previous)
    {
        public static readonly RuleTotals Empty = new(DateTime.MinValue, [], []);
    }
}

/// <summary>Hasil penerapan Volume &amp; Tiering.</summary>
/// <param name="Open">Kandidat, urut.</param>
/// <param name="Targeted">Biller yang mengejar target.</param>
/// <param name="AllLimited">Semua kandidat habis kuota sehingga kuota diabaikan.</param>
public sealed record CommitmentOrder<T>(IReadOnlyList<T> Open, IReadOnlySet<string> Targeted, bool AllLimited);
