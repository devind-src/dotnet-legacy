namespace SyncNetPro.Routing;

/// <summary>
/// Jendela waktu aturan jadwal (sama dengan dashboard Cek Jadwal): jam selesai eksklusif; selesai &lt; mulai = lewat tengah
/// malam (hari dicocokkan dengan hari mulai); selesai = mulai = 24 jam; tanggal bulanan yang tidak ada dilewati.
/// </summary>
public static class ScheduleWindow
{
    /// <summary>Aturan berlaku pada waktu <paramref name="t"/>.</summary>
    public static bool IsActiveAt(ScheduleRule rule, DateTime t)
    {
        ArgumentNullException.ThrowIfNull(rule);
        if (rule.IsOnce) return rule.StartAt is DateTime s && rule.EndAt is DateTime e && s <= t && t < e;

        // Jendela yang dimulai hari ini atau kemarin (lewat tengah malam) bisa memuat t.
        for (int back = 0; back <= 1; back++)
        {
            if (TryWindowOn(rule, t.Date.AddDays(-back), out DateTime start, out DateTime end) && start <= t && t < end) return true;
        }

        return false;
    }

    /// <summary>Senin = 1 .. Minggu = 7.</summary>
    public static int IsoDay(DateTime d) => d.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)d.DayOfWeek;

    private static bool TryWindowOn(ScheduleRule r, DateTime day, out DateTime start, out DateTime end)
    {
        start = end = default;
        if (r.TimeStart is not TimeSpan from || r.TimeEnd is not TimeSpan to) return false;
        if (r.ValidFrom is DateTime vf && day < vf.Date) return false;
        if (r.ValidUntil is DateTime vu && day > vu.Date) return false;

        bool match = r.Recurrence switch
        {
            ScheduleRecurrences.Daily => true,
            ScheduleRecurrences.Weekly => r.Days.Contains(IsoDay(day)),
            ScheduleRecurrences.Monthly => r.Days.Contains(day.Day),
            _ => false,
        };
        if (!match) return false;

        start = day.Date + from;
        end = to > from ? day.Date + to : day.Date.AddDays(1) + to;
        return true;
    }
}

/// <summary>
/// Jadwal Routing (Level 2): aturan dimuat saat start/RESYNC dan dievaluasi di memori per transaksi memakai jam server.
/// <list type="bullet">
/// <item>Tutup: biller dilewati selama jendela berlaku.</item>
/// <item>Jam Operasional: biller hanya buka di dalam jendela; aturan biller + produk menggantikan aturan semua produk.</item>
/// <item>Prioritas Jadwal: biller didahulukan; aturan per produk mengalahkan semua produk, lalu priority terkecil.</item>
/// </list>
/// </summary>
public sealed class RoutingSchedule(IRoutingScheduleStore store, TimeProvider time)
{
    private volatile IReadOnlyList<ScheduleRule> _rules = [];
    private volatile HashSet<string> _nodes = [];

    /// <summary>Waktu server (lokal).</summary>
    public DateTime Now => time.GetLocalNow().DateTime;

    /// <summary>Muat aturan.</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ScheduleRule> rules = await store.GetScheduleRulesAsync(Now, cancellationToken).ConfigureAwait(false);
        _rules = rules;
        _nodes = [.. rules.Where(r => !string.IsNullOrEmpty(r.NodeName)).Select(r => r.NodeName)];
    }

    /// <summary>Ada aturan apa pun untuk biller ini.</summary>
    public bool HasRulesFor(string? nodeName) => !string.IsNullOrEmpty(nodeName) && _nodes.Contains(nodeName);

    /// <summary><c>null</c> = biller buka; selain itu aturan penyebab tutup.</summary>
    public ScheduleRule? ClosedBy(string routingType, string? productId, string? nodeName, DateTime t)
    {
        if (!HasRulesFor(nodeName)) return null;
        IReadOnlyList<ScheduleRule> rules = _rules;

        ScheduleRule? closed = rules.FirstOrDefault(r => r.RuleType == ScheduleRuleTypes.Closed && r.NodeName == nodeName
            && InScope(r, routingType, productId) && ScheduleWindow.IsActiveAt(r, t));
        if (closed is not null) return closed;

        var open = rules.Where(r => r.RuleType == ScheduleRuleTypes.Open && r.NodeName == nodeName
            && (r.RoutingType is null || r.RoutingType == routingType)).ToList();
        var specific = open.Where(r => r.InstId == productId).ToList();
        List<ScheduleRule> applicable = specific.Count > 0 ? specific : [.. open.Where(r => r.InstId is null)];

        return applicable.Count > 0 && !applicable.Any(r => ScheduleWindow.IsActiveAt(r, t)) ? applicable[0] : null;
    }

    /// <summary>Priority jadwal yang berlaku untuk biller + produk, atau <c>null</c>.</summary>
    public short? SchedulePriority(string routingType, string? productId, string? nodeName, DateTime t)
    {
        if (!HasRulesFor(nodeName)) return null;
        return _rules.Where(r => r.RuleType == ScheduleRuleTypes.Priority && r.Priority.HasValue && r.NodeName == nodeName
                && InScope(r, routingType, productId) && ScheduleWindow.IsActiveAt(r, t))
            .OrderBy(r => r.InstId is null ? 1 : 0)
            .ThenBy(r => r.Priority)
            .FirstOrDefault()?.Priority;
    }

    /// <summary>
    /// Terapkan jadwal pada kandidat yang sudah urut menurut Routing Mode: buang yang tutup, lalu biller dengan
    /// Prioritas Jadwal dipindah ke depan (stabil). Semua tutup = <see cref="ScheduleOrder{T}.AllClosed"/>.
    /// </summary>
    public ScheduleOrder<T> Apply<T>(IReadOnlyList<T> ordered, Func<T, string> nodeName, string routingType, string? productId, bool usePriority = true)
    {
        ArgumentNullException.ThrowIfNull(ordered);
        ArgumentNullException.ThrowIfNull(nodeName);
        DateTime t = Now;
        var open = new List<(T Item, short? Priority, int Index)>();
        ScheduleRule? firstClosedBy = null;

        for (int i = 0; i < ordered.Count; i++)
        {
            T item = ordered[i];
            ScheduleRule? by = ClosedBy(routingType, productId, nodeName(item), t);
            if (by is not null)
            {
                firstClosedBy ??= by;
                continue;
            }

            open.Add((item, usePriority ? SchedulePriority(routingType, productId, nodeName(item), t) : null, i));
        }

        if (open.Count == 0) return new ScheduleOrder<T>([], true, false, firstClosedBy);

        var result = open.OrderBy(x => x.Priority.HasValue ? 0 : 1).ThenBy(x => x.Priority ?? 0).ThenBy(x => x.Index).ToList();
        return new ScheduleOrder<T>([.. result.Select(x => x.Item)], false, result[0].Priority.HasValue, null);
    }

    private static bool InScope(ScheduleRule r, string routingType, string? productId) =>
        (r.RoutingType is null || r.RoutingType == routingType) && (r.InstId is null || r.InstId == productId);
}

/// <summary>Hasil penerapan jadwal.</summary>
/// <param name="Open">Kandidat buka, urut.</param>
/// <param name="AllClosed">Tidak ada yang buka.</param>
/// <param name="FirstIsScheduled">Kandidat pertama berasal dari Prioritas Jadwal.</param>
/// <param name="ClosedBy">Aturan penyebab tutup kandidat pertama (bila semua tutup).</param>
public sealed record ScheduleOrder<T>(IReadOnlyList<T> Open, bool AllClosed, bool FirstIsScheduled, ScheduleRule? ClosedBy);
