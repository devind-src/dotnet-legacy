using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;

namespace SyncNetPro.Routing.Tests;

/// <summary>
/// Paritas logika routing dengan SyncNet.Routing (SDK lama): skenario golden (tools/SyncNetPro.GoldenGenerator/RoutingGolden.cs)
/// diputar ulang langkah demi langkah dengan store di memori dan jam tetap.
/// </summary>
public class LegacyParityTests
{
    private static JToken Golden(string file) => JToken.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Golden", file)));

    public static TheoryData<string> HealthScenarios() => Names("health.json");

    public static TheoryData<string> CommitmentScenarios() => Names("commitment.json");

    [Theory]
    [MemberData(nameof(HealthScenarios))]
    public async Task Health_check_matches_legacy(string name)
    {
        JObject scenario = Scenario("health.json", name);
        var time = new TestClock((DateTime)scenario["start"]!);
        var store = new HealthStore();
        foreach (JObject c in scenario["configs"]!.Cast<JObject>()) store.Configs.Add(Config(c));
        var tracker = new SupplierHealthTracker(store, time);
        await tracker.InitializeAsync(TestContext.Current.CancellationToken);

        var steps = scenario["steps"]!.Cast<JObject>().ToList();
        var results = scenario["results"]!.Cast<JObject>().ToList();
        for (int i = 0; i < steps.Count; i++)
        {
            JObject step = steps[i], expected = results[i];
            time.Advance(TimeSpan.FromMinutes((double)step["AdvanceMinutes"]!));
            store.Writes.Clear();
            string supplier = (string)step["Supplier"]!;
            if ((string)step["Action"]! == "reset")
            {
                await tracker.ResetAsync(supplier, "ops", TestContext.Current.CancellationToken);
            }
            else
            {
                await tracker.RecordResponseAsync(supplier, (string)step["RoutingType"]!, (bool)step["IsPayment"]!, (string?)step["Rc"], (int?)step["Latency"],
                    (string?)step["InstId"], 10000, "000001", cancellationToken: TestContext.Current.CancellationToken);
            }

            SupplierHealth s = tracker.GetStatus(supplier)!;
            string at = $"{name} langkah {i}";
            Assert.True(string.Equals((string)expected["status"]!, s.Status.ToString(), StringComparison.OrdinalIgnoreCase), at);
            Assert.Equal(((int)expected["ConsecutiveSuspectCount"]!, (int)expected["ConsecutiveFailedCount"]!, (int)expected["ConsecutivePendingCount"]!, (int)expected["ConsecutiveLatencyCount"]!),
                (s.ConsecutiveTimeoutCount, s.ConsecutiveFailedCount, s.ConsecutivePendingCount, s.ConsecutiveLatencyCount));
            Assert.Equal((string?)expected["BlockReason"], s.BlockReason);
            Assert.Equal((int)expected["RetryCount"]!, s.RetryCount);
            Assert.Equal((DateTime?)expected["BlockedSince"], s.BlockedSince);
            Assert.Equal((DateTime?)expected["BlockedUntil"], s.BlockedUntil);
            Assert.Equal((string?)expected["LastRcCode"], s.LastRcCode);
            Assert.Equal((int?)expected["LastLatencyMs"], s.LastLatencyMs);
            Assert.Equal((bool)expected["eligible"]!, tracker.IsEligible(supplier));
            time.Advance(TimeSpan.FromMinutes(1));
            Assert.Equal((bool)expected["eligibleIn1Min"]!, tracker.IsEligible(supplier));
            time.Advance(TimeSpan.FromMinutes(-1));
            Assert.Equal((bool)expected["failoverProduct"]!, tracker.IsFailoverEnabled(RoutingTypes.Product));
            Assert.Equal((bool)expected["failoverMargin"]!, tracker.IsFailoverEnabled(RoutingTypes.Margin));
            Assert.Equal(expected["writes"]!.Select(w => (string)w!), store.Writes);
        }
    }

    [Fact]
    public async Task Schedule_matches_legacy()
    {
        JObject golden = (JObject)Golden("schedule.json");
        var rules = golden["rules"]!.Cast<JObject>().Select(Rule).ToList();
        string[] nodes = [.. golden["nodes"]!.Select(n => (string)n!)];
        var time = new TestClock(new DateTime(2026, 9, 28, 8, 0, 0, DateTimeKind.Unspecified));
        var schedule = new RoutingSchedule(new ScheduleStore(rules), time);
        await schedule.InitializeAsync(TestContext.Current.CancellationToken);

        Assert.Equal(golden["hasRules"]!.Select(b => (bool)b!), nodes.Select(n => schedule.HasRulesFor(n)));

        foreach (JObject check in golden["checks"]!.Cast<JObject>())
        {
            DateTime at = (DateTime)check["at"]!;
            string rt = (string)check["routingType"]!, product = (string)check["product"]!;
            time.Now = at;
            string where = $"{at:s} {rt} {product}";

            Assert.True(check["closedBy"]!.Select(x => (int?)x).SequenceEqual(nodes.Select(n => schedule.ClosedBy(rt, product, n, at)?.Id)), where);
            Assert.True(check["priority"]!.Select(x => (int?)x).SequenceEqual(nodes.Select(n => (int?)schedule.SchedulePriority(rt, product, n, at))), where);

            ScheduleOrder<string> apply = schedule.Apply(nodes, n => n, rt, product);
            JObject expected = (JObject)check["apply"]!;
            Assert.Equal(expected["open"]!.Select(x => (string)x!), apply.Open);
            Assert.Equal((bool)expected["AllClosed"]!, apply.AllClosed);
            Assert.Equal((bool)expected["FirstIsScheduled"]!, apply.FirstIsScheduled);
            Assert.Equal((int?)expected["closedBy"], apply.ClosedBy?.Id);
        }

        time.Now = new DateTime(2026, 9, 28, 23, 30, 0, DateTimeKind.Unspecified);
        ScheduleOrder<string> allClosed = schedule.Apply(["BILLER_A"], n => n, RoutingTypes.Product, "PLN");
        Assert.Equal((bool)golden["allClosed"]!["AllClosed"]!, allClosed.AllClosed);
        Assert.Equal((int?)golden["allClosed"]!["closedBy"], allClosed.ClosedBy?.Id);
    }

    [Theory]
    [MemberData(nameof(CommitmentScenarios))]
    public async Task Volume_and_tiering_matches_legacy(string name)
    {
        JObject scenario = Scenario("commitment.json", name);
        var time = new TestClock((DateTime)scenario["start"]!);
        var store = new CommitmentStore();
        var rules = scenario["rules"]!.Cast<JObject>().Select(CommitmentRuleFrom).ToList();
        store.Rules.AddRange(rules);
        foreach (JObject v in scenario["seed"]!.Cast<JObject>())
        {
            store.Stored.Add(new VolumeDelta((DateTime)v["Date"]!, (string)v["RoutingType"]!, (string)v["NodeName"]!, (string)v["InstId"]!, (long)v["Count"]!, Dec(v["Amount"]!)));
        }

        await using var tracker = new CommitmentTracker(store, time, TimeSpan.Zero, NullLogger.Instance);
        await tracker.InitializeAsync(TestContext.Current.CancellationToken);

        var steps = scenario["steps"]!.Cast<JObject>().ToList();
        var results = scenario["results"]!.Cast<JObject>().ToList();
        for (int i = 0; i < steps.Count; i++)
        {
            JObject step = steps[i], expected = results[i];
            time.Advance(TimeSpan.FromMinutes((double)step["AdvanceMinutes"]!));
            store.Writes.Clear();
            string? product = (string?)step["Product"];
            string routingType = product == "TSEL10" ? RoutingTypes.Margin : RoutingTypes.Product;
            string at = $"{name} langkah {i} ({step["Action"]})";

            switch ((string)step["Action"]!)
            {
                case "init":
                    await tracker.InitializeAsync(TestContext.Current.CancellationToken);
                    break;
                case "record":
                    tracker.RecordVolume(routingType, (string?)step["Node"], product, (string?)step["TranType"], (string?)step["Rc"], Dec(step["Amount"]!),
                        (string?)step["SwitchKey"], (string?)step["OriginalKey"], (DateTime?)step["TranDate"]);
                    break;
                case "flush":
                    await tracker.FlushAsync(TestContext.Current.CancellationToken);
                    break;
                case "flush-fail":
                    store.FailNextAdd = true;
                    InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => tracker.FlushAsync(TestContext.Current.CancellationToken));
                    Assert.Equal((string)expected["result"]!, ex.Message);
                    break;
                case "apply":
                    var scheduled = step["Scheduled"]!.Select(x => (string)x!).ToHashSet();
                    CommitmentOrder<string> order = tracker.Apply([.. step["Candidates"]!.Select(x => (string)x!)], n => n, scheduled.Contains, routingType, product);
                    Assert.Equal(expected["result"]!["open"]!.Select(x => (string)x!), order.Open);
                    Assert.Equal(expected["result"]!["targeted"]!.Select(x => (string)x!), order.Targeted.Order(StringComparer.Ordinal));
                    Assert.Equal((bool)expected["result"]!["AllLimited"]!, order.AllLimited);
                    break;
            }

            foreach (CommitmentRule rule in rules)
            {
                Assert.True(Dec(expected["volumes"]![rule.Id.ToString(CultureInfo.InvariantCulture)]!) == tracker.Volume(rule, time.Now), $"{at} volume aturan {rule.Id}");
            }

            Assert.Equal(expected["writes"]!.Select(w => (string)w!), store.Writes);
        }
    }

    internal static decimal Dec(JToken token) => decimal.Parse(((decimal)token).ToString("G29", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

    private static TheoryData<string> Names(string file)
    {
        var data = new TheoryData<string>();
        foreach (JToken s in Golden(file)) data.Add((string)s["name"]!);
        return data;
    }

    private static JObject Scenario(string file, string name) => Golden(file).Cast<JObject>().Single(s => (string)s["name"]! == name);

    private static FailoverConfig Config(JObject c) => new()
    {
        RoutingType = (string)c["RoutingType"]!,
        InstId = (string?)c["InstId"],
        RcLinkDown = (string?)c["RcLinkDown"],
        LinkDownCooldownMinutes = (int?)c["LinkDownCooldownMinutes"],
        RcTimeout = (string?)c["RcSuspect"],
        MaxConsecutiveTimeout = (int)c["MaxConsecutiveSuspect"]!,
        TimeoutCooldownMinutes = (int?)c["SuspectCooldownMinutes"],
        RcFailed = (string?)c["RcFailed"],
        MaxConsecutiveFailed = (int)c["MaxConsecutiveFailed"]!,
        FailedCooldownMinutes = (int?)c["FailedCooldownMinutes"],
        RcPending = (string?)c["RcPending"],
        MaxConsecutivePending = (int)c["MaxConsecutivePending"]!,
        PendingCooldownMinutes = (int?)c["PendingCooldownMinutes"],
        LatencyThresholdMs = (int?)c["LatencyThresholdMs"],
        MaxConsecutiveLatency = (int)c["MaxConsecutiveLatency"]!,
        LatencyCooldownMinutes = (int?)c["LatencyCooldownMinutes"],
        IsActive = (bool)c["IsActive"]!,
    };

    private static ScheduleRule Rule(JObject r) => new()
    {
        Id = (int)r["Id"]!,
        RuleType = (string)r["RuleType"]!,
        RoutingType = (string?)r["RoutingType"],
        InstId = (string?)r["InstId"],
        NodeName = (string)r["NodeName"]!,
        Priority = (short?)r["Priority"],
        Recurrence = (string)r["Recurrence"]!,
        StartAt = (DateTime?)r["StartDt"],
        EndAt = (DateTime?)r["EndDt"],
        TimeStart = r["TimeStart"]!.Type == JTokenType.Null ? null : TimeSpan.Parse((string)r["TimeStart"]!, CultureInfo.InvariantCulture),
        TimeEnd = r["TimeEnd"]!.Type == JTokenType.Null ? null : TimeSpan.Parse((string)r["TimeEnd"]!, CultureInfo.InvariantCulture),
        Days = [.. r["Days"]!.Select(d => (int)d)],
        ValidFrom = (DateTime?)r["ValidFrom"],
        ValidUntil = (DateTime?)r["ValidUntil"],
    };

    private static CommitmentRule CommitmentRuleFrom(JObject r) => new()
    {
        Id = (int)r["Id"]!,
        RuleType = (string)r["RuleType"]!,
        RoutingType = (string)r["RoutingType"]!,
        NodeName = (string)r["NodeName"]!,
        InstId = (string?)r["InstId"],
        Metric = (string)r["Metric"]!,
        PeriodType = (string)r["PeriodType"]!,
        Threshold = Dec(r["Threshold"]!),
        WarnPct = (short?)r["WarnPct"],
        ValidFrom = (DateTime?)r["ValidFrom"],
        ValidUntil = (DateTime?)r["ValidUntil"],
        CreatedAt = (DateTime?)r["CreatedDt"],
    };
}
