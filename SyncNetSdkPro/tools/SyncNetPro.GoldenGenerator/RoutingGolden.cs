// Golden SyncNetPro.Routing (logika tanpa database): skenario dijalankan terhadap SyncNet.Routing (SDK lama) dengan
// store di memori dan jam tetap. Setiap skenario ditulis bersama spesifikasinya sehingga test SDK baru memutar ulang
// langkah yang sama dan membandingkan hasil per langkah.
using System.Text;
using Newtonsoft.Json;
using SyncNet.Models;
using SyncNet.Routing.Commitment;
using SyncNet.Routing.Failover;
using SyncNet.Routing.Schedule;

internal static class RoutingGolden
{
    private static readonly DateTime Start = new(2026, 9, 28, 8, 0, 0); // Senin

    public static void Write(string outDir)
    {
        Directory.CreateDirectory(outDir);
        var settings = new JsonSerializerSettings { Formatting = Formatting.Indented, DateFormatString = "yyyy-MM-ddTHH:mm:ss" };
        var utf8 = new UTF8Encoding(false);
        File.WriteAllText(Path.Combine(outDir, "health.json"), JsonConvert.SerializeObject(HealthScenarios(), settings), utf8);
        File.WriteAllText(Path.Combine(outDir, "schedule.json"), JsonConvert.SerializeObject(ScheduleScenarios(), settings), utf8);
        File.WriteAllText(Path.Combine(outDir, "commitment.json"), JsonConvert.SerializeObject(CommitmentScenarios(), settings), utf8);
        Console.WriteLine($"Golden routing (health, jadwal, volume) ditulis ke {outDir}");
    }

    // ------------------------------------------------------------------ health check

    public sealed record HealthStep(double AdvanceMinutes, string Action, string Supplier, string RoutingType, bool IsPayment, string Rc, int? Latency, string InstId);

    private sealed class HealthStore : ISupplierStatusStore
    {
        public List<FailoverModel.Config> Configs { get; } = [];
        public List<string> Writes { get; } = [];

        public Task<List<FailoverModel.SupplierStatus>> GetSupplierStatus() => Task.FromResult(new List<FailoverModel.SupplierStatus>());
        public Task<List<FailoverModel.Config>> GetFailoverConfig() => Task.FromResult(Configs);

        public Task UpsertSupplierStatus(FailoverModel.SupplierStatus s)
        {
            Writes.Add($"upsert {s.SupplierId} {s.Status} {s.BlockReason} {s.LastRcCode}");
            return Task.CompletedTask;
        }

        public Task InsertFailoverLog(string routingType, string instId, long? denom, string traceNumber, string fromSupplierId,
            string toSupplierId, string rcCode, string reason, int? latencyMs = null, int? scheduleId = null)
        {
            Writes.Add($"log {routingType} {instId} {fromSupplierId} {rcCode} {reason} {latencyMs} {scheduleId}");
            return Task.CompletedTask;
        }
    }

    private static List<object> HealthScenarios()
    {
        FailoverModel.Config Full(string instId = null, bool active = true) => new()
        {
            RoutingType = "PRODUCT", InstId = instId, RcLinkDown = "91,1091", LinkDownCooldownMinutes = 10,
            RcSuspect = "68, 1068", MaxConsecutiveSuspect = 3, SuspectCooldownMinutes = 5,
            RcFailed = "05,14,68", MaxConsecutiveFailed = 2, FailedCooldownMinutes = 3,
            RcPending = "09,68", MaxConsecutivePending = 2, PendingCooldownMinutes = 4,
            LatencyThresholdMs = 1000, MaxConsecutiveLatency = 2, LatencyCooldownMinutes = 2, IsActive = active,
        };

        HealthStep R(double adv, string rc, bool pay = true, int? lat = null, string supplier = "BILLER_A", string rt = "PRODUCT", string inst = "PLN") =>
            new(adv, "record", supplier, rt, pay, rc, lat, inst);
        HealthStep Reset(string supplier = "BILLER_A") => new(0, "reset", supplier, "PRODUCT", false, null, null, null);

        var scenarios = new List<(string Name, List<FailoverModel.Config> Configs, List<HealthStep> Steps)>
        {
            ("default-link-down-manual-reset", [], [R(0, "91"), R(1, "05"), R(1, "00", pay: false), R(1, "00"), R(0, "1091", pay: false), Reset()]),
            ("timeout-suspect-probe-cycle", [Full()], [R(0, "68"), R(1, "00", pay: false), R(1, "68"), R(1, "68"), R(1, "68"), R(6, "68"), R(6, "00", lat: 500), R(1, "68")]),
            ("inquiry-does-not-reset", [Full()], [R(0, "05", pay: false), R(0, "00", pay: false), R(0, "05", pay: false), R(10, "05"), R(0, "00"), R(0, "14"), R(0, "14")]),
            ("latency-and-timeout-not-latency", [Full()], [R(0, "00", lat: 1500), R(0, "68", lat: 5000), R(0, "00", lat: 900), R(0, "00", lat: 2000, pay: false), R(0, "00", lat: 2000), R(3, "00", lat: 100)]),
            ("pending-and-multi-category", [Full()], [R(0, "09"), R(0, "09"), R(5, "00"), R(0, "68"), R(0, "68")]),
            ("probe-slow-reblocks", [Full()], [R(0, "91"), R(11, "00", lat: 3000), R(0, "00", lat: 100), R(0, "00", lat: 100)]),
            ("in-window-payment-restores", [Full()], [R(0, "91"), R(1, "00", pay: false), R(1, "00", lat: 5000), R(1, "00", lat: 10)]),
            ("product-config-overrides-global", [Full(), With(Full("PLN"), c => { c.RcFailed = "77"; c.MaxConsecutiveFailed = 1; }), With(Full("BPJS", active: false), c => c.RcFailed = "77")],
                [R(0, "77"), R(0, "77", inst: "BPJS"), R(0, "05", inst: "BPJS"), R(0, "05", inst: "BPJS")]),
            ("margin-default-when-global-inactive", [With(Full(), c => { c.RoutingType = "MARGIN"; c.IsActive = false; })],
                [R(0, "68", rt: "MARGIN"), R(0, "68", rt: "MARGIN"), R(0, "68", rt: "MARGIN")]),
        };

        var output = new List<object>();
        foreach ((string name, List<FailoverModel.Config> configs, List<HealthStep> steps) in scenarios)
        {
            DateTime now = Start;
            var store = new HealthStore();
            store.Configs.AddRange(configs);
            var repo = new SupplierStatusRepository(store, () => now);
            repo.Initialize().GetAwaiter().GetResult();

            var results = new List<object>();
            foreach (HealthStep step in steps)
            {
                now = now.AddMinutes(step.AdvanceMinutes);
                store.Writes.Clear();
                if (step.Action == "reset") repo.ResetManualAsync(step.Supplier, "ops").GetAwaiter().GetResult();
                else repo.RecordResponseAsync(step.Supplier, step.RoutingType, step.IsPayment, step.Rc, step.Latency, step.InstId, 10000, "000001").GetAwaiter().GetResult();

                var s = repo.GetStatus(step.Supplier);
                results.Add(new
                {
                    status = s.Status.ToString(), s.ConsecutiveSuspectCount, s.ConsecutiveFailedCount, s.ConsecutivePendingCount, s.ConsecutiveLatencyCount,
                    s.BlockReason, s.RetryCount, s.BlockedSince, s.BlockedUntil, s.LastRcCode, s.LastLatencyMs,
                    eligible = repo.IsEligible(step.Supplier),
                    eligibleIn1Min = EligibleAt(repo, step.Supplier, ref now, 1),
                    failoverProduct = repo.IsFailoverEnabled("PRODUCT"), failoverMargin = repo.IsFailoverEnabled("MARGIN"),
                    writes = store.Writes.ToList(),
                });
            }

            output.Add(new { name, start = Start, configs, steps, results });
        }

        return output;
    }

    private static FailoverModel.Config With(FailoverModel.Config c, Action<FailoverModel.Config> change)
    {
        change(c);
        return c;
    }

    // IsEligible 1 menit ke depan tanpa mengubah jam skenario.
    private static bool EligibleAt(SupplierStatusRepository repo, string supplier, ref DateTime now, double minutes)
    {
        DateTime saved = now;
        now = now.AddMinutes(minutes);
        bool eligible = repo.IsEligible(supplier);
        now = saved;
        return eligible;
    }

    // ------------------------------------------------------------------ jadwal

    private sealed class ScheduleStore(List<RoutingScheduleModel> rules) : IRoutingScheduleStore
    {
        public Task<List<RoutingScheduleModel>> GetRoutingSchedules() => Task.FromResult(rules);
    }

    private static object ScheduleScenarios()
    {
        var rules = new List<RoutingScheduleModel>
        {
            new() { Id = 1, RuleType = "CLOSED", NodeName = "BILLER_A", Recurrence = "DAILY", TimeStart = new TimeSpan(23, 0, 0), TimeEnd = new TimeSpan(1, 0, 0) },
            new() { Id = 2, RuleType = "OPEN", NodeName = "BILLER_B", Recurrence = "WEEKLY", Days = [1, 2, 3, 4, 5], TimeStart = new TimeSpan(8, 0, 0), TimeEnd = new TimeSpan(17, 0, 0) },
            new() { Id = 3, RuleType = "OPEN", NodeName = "BILLER_B", InstId = "BPJS", Recurrence = "DAILY", TimeStart = new TimeSpan(6, 0, 0), TimeEnd = new TimeSpan(6, 0, 0) },
            new() { Id = 4, RuleType = "PRIORITY", NodeName = "BILLER_C", Priority = 2, Recurrence = "DAILY", TimeStart = new TimeSpan(9, 0, 0), TimeEnd = new TimeSpan(12, 0, 0) },
            new() { Id = 5, RuleType = "PRIORITY", NodeName = "BILLER_C", InstId = "PLN", Priority = 5, Recurrence = "DAILY", TimeStart = new TimeSpan(10, 0, 0), TimeEnd = new TimeSpan(11, 0, 0) },
            new() { Id = 6, RuleType = "PRIORITY", NodeName = "BILLER_D", Priority = 1, RoutingType = "MARGIN", Recurrence = "MONTHLY", Days = [1, 15, 31], TimeStart = new TimeSpan(0, 0, 0), TimeEnd = new TimeSpan(0, 0, 0) },
            new() { Id = 7, RuleType = "CLOSED", NodeName = "BILLER_D", Recurrence = "ONCE", StartDt = new DateTime(2026, 9, 30, 20, 0, 0), EndDt = new DateTime(2026, 10, 1, 2, 0, 0) },
            new() { Id = 8, RuleType = "CLOSED", NodeName = "BILLER_E", RoutingType = "PRODUCT", InstId = "PLN", Recurrence = "WEEKLY", Days = [7], TimeStart = new TimeSpan(22, 0, 0), TimeEnd = new TimeSpan(2, 0, 0),
                ValidFrom = new DateTime(2026, 9, 1), ValidUntil = new DateTime(2026, 10, 31) },
            new() { Id = 9, RuleType = "PRIORITY", NodeName = "BILLER_E", Priority = 3, Recurrence = "DAILY", TimeStart = new TimeSpan(0, 0, 0), TimeEnd = new TimeSpan(23, 59, 0), ValidFrom = new DateTime(2026, 10, 1) },
        };

        string[] nodes = ["BILLER_A", "BILLER_B", "BILLER_C", "BILLER_D", "BILLER_E", "BILLER_X"];
        string[] products = ["PLN", "BPJS", "PDAM"];
        string[] routingTypes = ["PRODUCT", "MARGIN"];
        var times = new List<DateTime>();
        for (DateTime t = new(2026, 9, 26, 0, 30, 0); t < new DateTime(2026, 10, 2, 0, 0, 0); t = t.AddMinutes(170)) times.Add(t);
        times.AddRange([new DateTime(2026, 9, 28, 23, 0, 0), new DateTime(2026, 9, 29, 1, 0, 0), new DateTime(2026, 9, 28, 17, 0, 0), new DateTime(2026, 10, 1, 1, 59, 59), new DateTime(2026, 10, 4, 23, 30, 0)]);

        DateTime now = Start;
        var repo = new RoutingScheduleRepository(new ScheduleStore(rules), () => now);
        repo.Initialize().GetAwaiter().GetResult();

        var checks = new List<object>();
        foreach (DateTime t in times)
        {
            now = t;
            foreach (string rt in routingTypes)
            foreach (string product in products)
            {
                var apply = repo.Apply(nodes, n => n, rt, product);
                checks.Add(new
                {
                    at = t, routingType = rt, product,
                    closedBy = nodes.Select(n => repo.ClosedBy(rt, product, n, t)?.Id).ToArray(),
                    priority = nodes.Select(n => (int?)repo.SchedulePriority(rt, product, n, t)).ToArray(),
                    apply = new { open = apply.Open, apply.AllClosed, apply.FirstIsScheduled, closedBy = apply.ClosedBy?.Id },
                });
            }
        }

        now = new DateTime(2026, 9, 28, 23, 30, 0);
        var allClosed = repo.Apply(new[] { "BILLER_A", "BILLER_D" }.Take(1).ToList(), n => n, "PRODUCT", "PLN");
        return new { rules, nodes, hasRules = nodes.Select(repo.HasRulesFor).ToArray(), checks, allClosed = new { allClosed.AllClosed, closedBy = allClosed.ClosedBy?.Id } };
    }

    // ------------------------------------------------------------------ Volume & Tiering

    private sealed class CommitmentStore : ICommitmentStore
    {
        public bool Active { get; set; } = true;
        public List<CommitmentRuleModel> Rules { get; } = [];
        public List<VolumeModel> Stored { get; } = [];
        public List<string> Writes { get; } = [];
        public bool FailNextAdd { get; set; }

        public Task<bool> IsCommitmentActive() => Task.FromResult(Active);
        public Task<List<CommitmentRuleModel>> GetCommitmentRules() => Task.FromResult(Rules.ToList());

        public Task<List<VolumeTotalModel>> GetVolumeTotals(DateTime today, IReadOnlyCollection<string> nodeNames)
        {
            var month = new DateTime(today.Year, today.Month, 1);
            var prevMonth = month.AddMonths(-1);
            return Task.FromResult(Stored.Where(v => nodeNames.Contains(v.NodeName) && v.Date >= prevMonth && v.Date <= today)
                .GroupBy(v => (v.RoutingType, v.NodeName, v.InstId))
                .Select(g => new VolumeTotalModel
                {
                    RoutingType = g.Key.RoutingType, NodeName = g.Key.NodeName, InstId = g.Key.InstId,
                    TodayCount = g.Where(v => v.Date == today).Sum(v => v.Count), TodayAmount = g.Where(v => v.Date == today).Sum(v => v.Amount),
                    YesterdayCount = g.Where(v => v.Date == today.AddDays(-1)).Sum(v => v.Count), YesterdayAmount = g.Where(v => v.Date == today.AddDays(-1)).Sum(v => v.Amount),
                    MonthCount = g.Where(v => v.Date >= month).Sum(v => v.Count), MonthAmount = g.Where(v => v.Date >= month).Sum(v => v.Amount),
                    PrevMonthCount = g.Where(v => v.Date < month).Sum(v => v.Count), PrevMonthAmount = g.Where(v => v.Date < month).Sum(v => v.Amount),
                }).ToList());
        }

        public Task AddVolumes(IReadOnlyList<VolumeModel> deltas)
        {
            if (FailNextAdd)
            {
                FailNextAdd = false;
                throw new InvalidOperationException("db down");
            }

            foreach (VolumeModel d in deltas.OrderBy(d => d.Date).ThenBy(d => d.NodeName).ThenBy(d => d.InstId))
            {
                Writes.Add($"volume {d.Date:yyyy-MM-dd} {d.RoutingType} {d.NodeName} {d.InstId} {d.Count} {d.Amount}");
                var row = Stored.FirstOrDefault(v => v.Date == d.Date && v.RoutingType == d.RoutingType && v.NodeName == d.NodeName && v.InstId == d.InstId);
                if (row is null) Stored.Add(row = new VolumeModel { Date = d.Date, RoutingType = d.RoutingType, NodeName = d.NodeName, InstId = d.InstId });
                row.Count += d.Count;
                row.Amount += d.Amount;
            }

            return Task.CompletedTask;
        }

        public Task InsertCommitmentLogs(IReadOnlyList<CommitmentLogModel> logs)
        {
            foreach (CommitmentLogModel l in logs.OrderBy(l => l.CommitmentId).ThenBy(l => l.Event))
                Writes.Add($"event {l.CommitmentId} {l.Event} {l.PeriodStart:yyyy-MM-dd} {l.VolumeValue} {l.ThresholdValue} {l.InstId}");
            return Task.CompletedTask;
        }
    }

    public sealed record CommitmentStep(double AdvanceMinutes, string Action, string Node, string Product, string TranType, string Rc, decimal Amount,
        string SwitchKey, string OriginalKey, DateTime? TranDate, string[] Candidates, string[] Scheduled);

    private static List<object> CommitmentScenarios()
    {
        CommitmentStep Rec(string node, string tran, decimal amount = 10000, string key = null, string orig = null, string rc = "00", string product = "PLN", DateTime? date = null, double adv = 0) =>
            new(adv, "record", node, product, tran, rc, amount, key, orig, date, null, null);
        CommitmentStep Flush(double adv = 0) => new(adv, "flush", null, null, null, null, 0, null, null, null, null, null);
        CommitmentStep FailFlush() => new(0, "flush-fail", null, null, null, null, 0, null, null, null, null, null);
        CommitmentStep Apply(string product, string[] candidates, string[] scheduled = null, double adv = 0) =>
            new(adv, "apply", null, product, null, null, 0, null, null, null, candidates, scheduled ?? []);
        CommitmentStep Init() => new(0, "init", null, null, null, null, 0, null, null, null, null, null);

        var rules = new List<CommitmentRuleModel>
        {
            new() { Id = 1, RuleType = "LIMIT", RoutingType = "PRODUCT", NodeName = "BILLER_A", Metric = "COUNT", PeriodType = "DAILY", Threshold = 3, WarnPct = 60 },
            new() { Id = 2, RuleType = "TARGET", RoutingType = "PRODUCT", NodeName = "BILLER_B", InstId = "PLN", Metric = "AMOUNT", PeriodType = "MONTHLY", Threshold = 50000,
                CreatedDt = new DateTime(2026, 8, 1) },
            new() { Id = 3, RuleType = "LIMIT", RoutingType = "PRODUCT", NodeName = "BILLER_C", Metric = "AMOUNT", PeriodType = "MONTHLY", Threshold = 20000,
                ValidFrom = new DateTime(2026, 9, 1), ValidUntil = new DateTime(2026, 9, 30) },
            new() { Id = 4, RuleType = "TARGET", RoutingType = "MARGIN", NodeName = "BILLER_A", Metric = "COUNT", PeriodType = "DAILY", Threshold = 2 },
        };

        string[] abc = ["BILLER_A", "BILLER_B", "BILLER_C"];
        var scenarios = new List<(string Name, List<VolumeModel> Seed, List<CommitmentStep> Steps)>
        {
            ("limit-warn-reached-and-ignored", [], [
                Apply("PLN", abc), Rec("BILLER_A", "PAY", key: "K1"), Rec("BILLER_A", "PAY", key: "K1"), Rec("BILLER_A", "PAY", key: "K2"),
                Apply("PLN", abc), Rec("BILLER_A", "PAY", key: "K3"), Apply("PLN", abc), Apply("PLN", ["BILLER_A"]), Flush(), Apply("PLN", ["BILLER_A", "BILLER_C"], ["BILLER_C"]),
            ]),
            ("advice-reversal-and-failed", [], [
                Rec("BILLER_B", "PAY", 20000, key: "P1"), Rec("BILLER_B", "ADV", 20000, orig: "P1"), Rec("BILLER_B", "ADV", 15000, orig: "P2"),
                Rec("BILLER_B", "ADV", 15000, orig: "P2"), Rec("BILLER_B", "PAY", 5000, key: "P3", rc: "05"), Rec("BILLER_B", "REV", 20000, orig: "P1"),
                Rec("BILLER_B", "REV", 20000, orig: "P9"), Rec("BILLER_B", "INQ", 1000, key: "I1"), Flush(), Apply("PLN", abc), Apply("BPJS", abc),
            ]),
            ("seeded-totals-and-period-rollover", [
                new() { Date = new DateTime(2026, 9, 28), RoutingType = "PRODUCT", NodeName = "BILLER_A", InstId = "PLN", Count = 2, Amount = 20000 },
                new() { Date = new DateTime(2026, 9, 10), RoutingType = "PRODUCT", NodeName = "BILLER_B", InstId = "PLN", Count = 4, Amount = 49000 },
                new() { Date = new DateTime(2026, 9, 15), RoutingType = "PRODUCT", NodeName = "BILLER_C", InstId = "PDAM", Count = 1, Amount = 25000 },
                new() { Date = new DateTime(2026, 8, 20), RoutingType = "PRODUCT", NodeName = "BILLER_B", InstId = "PLN", Count = 1, Amount = 60000 },
            ], [
                Init(), Apply("PLN", abc), Rec("BILLER_B", "PAY", 1000, key: "S1"), Apply("PLN", abc), Rec("BILLER_A", "PAY", key: "S2", date: new DateTime(2026, 9, 27)),
                Rec("BILLER_A", "PAY", key: "S3", date: new DateTime(2026, 9, 1)), Flush(), Apply("PLN", abc, adv: 60 * 24 * 3), Flush(), Apply("PLN", abc),
            ]),
            ("flush-failure-keeps-deltas", [], [
                Rec("BILLER_A", "PAY", key: "F1"), FailFlush(), Apply("PLN", abc), Rec("BILLER_A", "PAY", key: "F2"), Flush(), Apply("PLN", abc),
            ]),
            ("margin-target", [], [
                Apply("TSEL10", ["BILLER_B", "BILLER_A"]), Rec("BILLER_A", "PAY", key: "M1", product: "TSEL10") with { },
                Apply("TSEL10", ["BILLER_B", "BILLER_A"]), Flush(),
            ]),
        };

        var output = new List<object>();
        foreach ((string name, List<VolumeModel> seed, List<CommitmentStep> steps) in scenarios)
        {
            DateTime now = Start.AddHours(2);
            var store = new CommitmentStore();
            store.Rules.AddRange(rules);
            store.Stored.AddRange(seed.Select(v => new VolumeModel { Date = v.Date, RoutingType = v.RoutingType, NodeName = v.NodeName, InstId = v.InstId, Count = v.Count, Amount = v.Amount }));
            var repo = new CommitmentRepository(store, () => now, 0);
            repo.Initialize().GetAwaiter().GetResult();

            var results = new List<object>();
            foreach (CommitmentStep step in steps)
            {
                now = now.AddMinutes(step.AdvanceMinutes);
                store.Writes.Clear();
                object result = null;
                string routingType = step.Product == "TSEL10" ? "MARGIN" : "PRODUCT";
                switch (step.Action)
                {
                    case "init": repo.Initialize().GetAwaiter().GetResult(); break;
                    case "record": repo.RecordVolume(routingType, step.Node, step.Product, step.TranType, step.Rc, step.Amount, step.SwitchKey, step.OriginalKey, step.TranDate); break;
                    case "flush": repo.FlushAsync().GetAwaiter().GetResult(); break;
                    case "flush-fail":
                        store.FailNextAdd = true;
                        try { repo.FlushAsync().GetAwaiter().GetResult(); result = "no-error"; }
                        catch (InvalidOperationException ex) { result = ex.Message; }
                        break;
                    case "apply":
                        var scheduled = step.Scheduled.ToHashSet();
                        var order = repo.Apply(step.Candidates, n => n, n => scheduled.Contains(n), routingType, step.Product);
                        result = new { open = order.Open, targeted = order.Targeted.OrderBy(x => x).ToArray(), order.AllLimited };
                        break;
                }

                results.Add(new
                {
                    result,
                    volumes = rules.ToDictionary(r => r.Id, r => repo.Volume(r, now)),
                    writes = store.Writes.ToList(),
                });
            }

            output.Add(new { name, start = Start.AddHours(2), rules, seed, steps, results });
        }

        return output;
    }
}
