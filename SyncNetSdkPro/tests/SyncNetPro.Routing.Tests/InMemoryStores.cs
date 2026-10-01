using System.Globalization;

namespace SyncNetPro.Routing.Tests;

/// <summary>Jam uji yang dapat maju/mundur (golden memeriksa titik waktu acak). Waktu lokal = UTC.</summary>
internal sealed class TestClock(DateTime now) : TimeProvider
{
    public DateTime Now { get; set; } = DateTime.SpecifyKind(now, DateTimeKind.Unspecified);

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(Now, DateTimeKind.Utc));

    public void Advance(TimeSpan delta) => Now += delta;
}

internal sealed class HealthStore : ISupplierHealthStore
{
    public List<FailoverConfig> Configs { get; } = [];

    public List<SupplierHealth> Initial { get; } = [];

    public List<string> Writes { get; } = [];

    public Task<IReadOnlyList<SupplierHealth>> GetSupplierHealthAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<SupplierHealth>>(Initial);

    public Task<IReadOnlyList<FailoverConfig>> GetFailoverConfigAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<FailoverConfig>>(Configs);

    public Task UpsertSupplierHealthAsync(SupplierHealth health, CancellationToken cancellationToken)
    {
        Writes.Add($"upsert {health.SupplierId} {health.Status.ToString().ToUpperInvariant()} {health.BlockReason} {health.LastRcCode}");
        return Task.CompletedTask;
    }

    public Task InsertFailoverLogAsync(FailoverLogEntry e, CancellationToken cancellationToken)
    {
        Writes.Add($"log {e.RoutingType} {e.InstId} {e.FromSupplierId} {e.RcCode} {e.Reason} {e.LatencyMs} {e.ScheduleId}");
        return Task.CompletedTask;
    }
}

internal sealed class ScheduleStore(IReadOnlyList<ScheduleRule> rules) : IRoutingScheduleStore
{
    public Task<IReadOnlyList<ScheduleRule>> GetScheduleRulesAsync(DateTime now, CancellationToken cancellationToken) => Task.FromResult(rules);
}

internal sealed class CommitmentStore : ICommitmentStore
{
    public bool Active { get; set; } = true;

    public List<CommitmentRule> Rules { get; } = [];

    public List<VolumeDelta> Stored { get; } = [];

    public List<string> Writes { get; } = [];

    public bool FailNextAdd { get; set; }

    public Task<bool> IsCommitmentActiveAsync(CancellationToken cancellationToken) => Task.FromResult(Active);

    public Task<IReadOnlyList<CommitmentRule>> GetCommitmentRulesAsync(DateTime today, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<CommitmentRule>>([.. Rules]);

    public Task<IReadOnlyList<VolumeTotal>> GetVolumeTotalsAsync(DateTime today, IReadOnlyCollection<string> nodeNames, CancellationToken cancellationToken)
    {
        var month = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        DateTime prevMonth = month.AddMonths(-1);
        return Task.FromResult<IReadOnlyList<VolumeTotal>>([.. Stored.Where(v => nodeNames.Contains(v.NodeName) && v.Date >= prevMonth && v.Date <= today)
            .GroupBy(v => (v.RoutingType, v.NodeName, v.InstId))
            .Select(g => new VolumeTotal
            {
                RoutingType = g.Key.RoutingType,
                NodeName = g.Key.NodeName,
                InstId = g.Key.InstId,
                TodayCount = g.Where(v => v.Date == today).Sum(v => v.Count),
                TodayAmount = g.Where(v => v.Date == today).Sum(v => v.Amount),
                YesterdayCount = g.Where(v => v.Date == today.AddDays(-1)).Sum(v => v.Count),
                YesterdayAmount = g.Where(v => v.Date == today.AddDays(-1)).Sum(v => v.Amount),
                MonthCount = g.Where(v => v.Date >= month).Sum(v => v.Count),
                MonthAmount = g.Where(v => v.Date >= month).Sum(v => v.Amount),
                PreviousMonthCount = g.Where(v => v.Date < month).Sum(v => v.Count),
                PreviousMonthAmount = g.Where(v => v.Date < month).Sum(v => v.Amount),
            })]);
    }

    public Task AddVolumesAsync(IReadOnlyList<VolumeDelta> deltas, CancellationToken cancellationToken)
    {
        if (FailNextAdd)
        {
            FailNextAdd = false;
            throw new InvalidOperationException("db down");
        }

        foreach (VolumeDelta d in deltas.OrderBy(d => d.Date).ThenBy(d => d.NodeName, StringComparer.Ordinal).ThenBy(d => d.InstId, StringComparer.Ordinal))
        {
            Writes.Add(string.Create(CultureInfo.InvariantCulture, $"volume {d.Date:yyyy-MM-dd} {d.RoutingType} {d.NodeName} {d.InstId} {d.Count} {d.Amount}"));
            int i = Stored.FindIndex(v => v.Date == d.Date && v.RoutingType == d.RoutingType && v.NodeName == d.NodeName && v.InstId == d.InstId);
            if (i < 0) Stored.Add(d);
            else Stored[i] = Stored[i] with { Count = Stored[i].Count + d.Count, Amount = Stored[i].Amount + d.Amount };
        }

        return Task.CompletedTask;
    }

    public Task InsertCommitmentEventsAsync(IReadOnlyList<CommitmentEvent> events, CancellationToken cancellationToken)
    {
        foreach (CommitmentEvent e in events.OrderBy(e => e.CommitmentId).ThenBy(e => e.Event, StringComparer.Ordinal))
        {
            Writes.Add(string.Create(CultureInfo.InvariantCulture, $"event {e.CommitmentId} {e.Event} {e.PeriodStart:yyyy-MM-dd} {e.Volume} {e.Threshold} {e.InstId}"));
        }

        return Task.CompletedTask;
    }
}
