using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using Npgsql;
using SyncNetPro.Contracts;

namespace SyncNetPro.Routing.Tests;

/// <summary>
/// Resolver routing + fee end-to-end di PostgreSQL: langkah dan hasil dari SDK lama (golden <c>resolver.json</c>, data
/// <c>Sql/seed.sql</c>) diputar ulang dengan <see cref="PostgresRoutingStore"/>. Butuh <c>SYNCNET_TEST_PG</c>.
/// </summary>
public sealed class ResolverParityTests : IAsyncLifetime
{
    private static readonly string? ConnectionString = Environment.GetEnvironmentVariable("SYNCNET_TEST_PG");
    private readonly string _schema = "routing_test_" + Guid.NewGuid().ToString("N")[..12];
    private NpgsqlDataSource? _dataSource;

    public async ValueTask InitializeAsync()
    {
        if (string.IsNullOrEmpty(ConnectionString)) return;
        await using (var admin = new NpgsqlConnection(ConnectionString))
        {
            await admin.OpenAsync();
            string sql = $"CREATE SCHEMA {_schema}; SET search_path TO {_schema};"
                + await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Sql", "schema.sql"))
                + await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Sql", "seed.sql"));
            await using var cmd = new NpgsqlCommand(sql, admin);
            await cmd.ExecuteNonQueryAsync();
        }

        _dataSource = NpgsqlDataSource.Create(new NpgsqlConnectionStringBuilder(ConnectionString) { SearchPath = _schema }.ConnectionString);
    }

    public async ValueTask DisposeAsync()
    {
        if (_dataSource is null) return;
        await _dataSource.DisposeAsync();
        await using var admin = new NpgsqlConnection(ConnectionString);
        await admin.OpenAsync();
        await using var cmd = new NpgsqlCommand($"DROP SCHEMA IF EXISTS {_schema} CASCADE", admin);
        await cmd.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task Resolver_and_fees_match_legacy_on_postgres()
    {
        Assert.SkipWhen(_dataSource is null, "Set SYNCNET_TEST_PG untuk menjalankan test PostgreSQL");
        CancellationToken ct = TestContext.Current.CancellationToken;
        // Tanggal dibiarkan sebagai teks agar perbandingan tabel tidak bergantung pada culture.
        using var json = new Newtonsoft.Json.JsonTextReader(new StringReader(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Golden", "resolver.json"), ct)))
        {
            DateParseHandling = Newtonsoft.Json.DateParseHandling.None,
        };
        JObject golden = JObject.Load(json);

        var time = new TestClock(DateTime.Parse((string)golden["now"]!, CultureInfo.InvariantCulture));
        var store = new PostgresRoutingStore(_dataSource!, time, NullLogger<PostgresRoutingStore>.Instance);
        var health = new SupplierHealthTracker(store, time);
        var schedule = new RoutingSchedule(store, time);
        await using var commitment = new CommitmentTracker(store, time, TimeSpan.Zero, NullLogger.Instance);
        var prices = new PriceBook(store);
        var fees = new ProductFeeCalculator(store);
        var margin = new MarginCalculator(prices);
        var resolver = new RoutingResolver(new StaticRouting(store), new MarginRouting(store, prices, health, schedule, commitment),
            new ProductRouting(store, health, schedule, commitment), health, schedule, store, NullLogger<RoutingResolver>.Instance, commitment);

        await fees.InitializeAsync(ct);
        await prices.InitializeAsync(ct);
        await resolver.InitializeAsync(ct);

        var steps = golden["steps"]!.Cast<JObject>().ToList();
        var results = golden["results"]!.ToList();
        for (int i = 0; i < steps.Count; i++)
        {
            JObject s = steps[i];
            JToken expected = results[i];
            string op = (string)s["Op"]!;
            string where = $"langkah {i} ({op} {s["TranType"]} {s["Product"]} {s["Merchant"]})";
            var ctx = new RoutingContext
            {
                TranType = (string?)s["TranType"],
                ProductId = (string?)s["Product"] ?? string.Empty,
                Denom = (long)s["Denom"]!,
                MerchantId = (string?)s["Merchant"],
                TerminalId = (string?)s["Terminal"],
                Refnum = (string?)s["Refnum"],
                TransactionDateTime = (string?)s["DateTime"],
                TraceNumber = (string?)s["Trace"],
                OriginalData = (string?)s["OriginalData"],
            };

            switch (op)
            {
                case "mode":
                    (string mode, int? node) = fees.GetRoutingMode(ctx.ProductId, ctx.MerchantId, (string?)s["SubMerchant"]);
                    Assert.True((string)expected["mode"]! == mode && (int?)expected["staticNodeId"] == node, where);
                    break;
                case "product":
                    (string m, int? n) = fees.GetRoutingMode(ctx.ProductId, ctx.MerchantId);
                    AssertDecision(expected, await resolver.ResolveProductAsync(ctx, m, n, ct), where);
                    break;
                case "margin":
                    AssertDecision(expected, await resolver.ResolveMarginAsync(ctx, ct), where);
                    break;
                case "static":
                    Assert.True((string?)expected == resolver.ResolveStaticSupplier(ctx.ProductId), where);
                    break;
                case "margin-fee":
                    AssertFees(expected, margin.Calculate(ctx.MerchantId, ctx.ProductId, ctx.Denom, (string?)s["Supplier"]), where);
                    break;
                case "fees":
                    AssertFees(expected, fees.Calculate(ctx.MerchantId, ctx.ProductId, LegacyParityTests.Dec(s["Amount"]!), (int?)s["Sharing"], (string?)s["SubMerchant"]), where);
                    break;
                case "record":
                    await resolver.RecordResultAsync((string)s["Supplier"]!, (string)s["RoutingType"]!, ctx.TranType, (string?)s["Rc"], ctx.ProductId, ctx.Denom,
                        ctx.TraceNumber, (int?)s["Latency"], LegacyParityTests.Dec(s["Amount"]!),
                        ctx.TranType == TranType.Payment ? SwitchKeys.Build(ctx.TranType, ctx.TransactionDateTime, ctx.TraceNumber, ctx.TerminalId) : null,
                        null, SwitchKeys.ParseTransactionDate(ctx.TransactionDateTime), ct);
                    break;
                case "volume":
                    resolver.RecordVolume((string)s["Supplier"]!, (string)s["RoutingType"]!, ctx.TranType, (string?)s["Rc"], ctx.ProductId, LegacyParityTests.Dec(s["Amount"]!),
                        SwitchKeys.Build(ctx.TranType, ctx.TransactionDateTime, ctx.TraceNumber, ctx.TerminalId), null, SwitchKeys.ParseTransactionDate(ctx.TransactionDateTime));
                    break;
                case "flush":
                    await resolver.FlushVolumeAsync(ct);
                    break;
                default:
                    throw new InvalidOperationException(op);
            }
        }

        JObject tables = (JObject)golden["tables"]!;
        await AssertTableAsync(tables["tranMap"]!, "SELECT merchant_id,terminal_id,refnum,routing_type,inst_id,denom,node_name,inquiry_switch_key,switch_key FROM sw_routes_tran_map ORDER BY id");
        await AssertTableAsync(tables["failoverLog"]!, "SELECT routing_type,inst_id,denom,trace_number,from_supplier_id,to_supplier_id,rc_code,reason,latency_ms,schedule_id FROM sw_routes_failover_log ORDER BY id");
        await AssertTableAsync(tables["supplierStatus"]!, "SELECT supplier_id,status,last_rc_code,consecutive_suspect_count,consecutive_failed_count,consecutive_pending_count,consecutive_latency_count,block_reason,last_latency_ms,retry_count,last_tran_dt,blocked_since,blocked_until,updated_by,updated_dt FROM sw_routes_supplier_status ORDER BY supplier_id");
        await AssertTableAsync(tables["volume"]!, "SELECT volume_date,routing_type,node_name,inst_id,tran_count,tran_amount FROM sw_routes_volume ORDER BY volume_date,routing_type,node_name,inst_id");
        await AssertTableAsync(tables["commitmentLog"]!, "SELECT commitment_id,rule_type,routing_type,node_name,inst_id,metric,period_type,period_start,event,volume_value,threshold_value FROM sw_routes_commitment_log ORDER BY id");
    }

    private static void AssertDecision(JToken expected, RoutingDecision actual, string where) =>
        Assert.True(
            (bool)expected["Apply"]! == actual.Apply && (string)expected["NodeName"]! == actual.NodeName
                && (bool)expected["Rejected"]! == actual.Rejected && (int?)expected["ScheduleId"] == actual.ScheduleId,
            $"{where}: diharapkan {expected.ToString(Newtonsoft.Json.Formatting.None)}, didapat {actual}");

    private static void AssertFees(JToken expected, Fees actual, string where)
    {
        var e = (JObject)expected;
        Assert.True(
            LegacyParityTests.Dec(e["total_fee"]!) == actual.TotalFee && LegacyParityTests.Dec(e["acquirer_fee"]!) == actual.AcquirerFee
                && LegacyParityTests.Dec(e["merchant_fee"]!) == actual.MerchantFee && LegacyParityTests.Dec(e["issuer_fee"]!) == actual.IssuerFee
                && LegacyParityTests.Dec(e["biller_fee"]!) == actual.BillerFee && LegacyParityTests.Dec(e["switch_fee"]!) == actual.SwitchFee
                && LegacyParityTests.Dec(e["submerchant_fee"]!) == actual.SubmerchantFee,
            $"{where}: diharapkan {e.ToString(Newtonsoft.Json.Formatting.None)}, didapat total {actual.TotalFee} biller {actual.BillerFee} switch {actual.SwitchFee}");
    }

    private async Task AssertTableAsync(JToken expected, string sql)
    {
        await using NpgsqlConnection conn = await _dataSource!.OpenConnectionAsync(TestContext.Current.CancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        var actual = new List<Dictionary<string, string?>>();
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            var row = new Dictionary<string, string?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                object? v = reader.IsDBNull(i) ? null : reader.GetValue(i);
                row[reader.GetName(i)] = v switch
                {
                    null => null,
                    DateOnly day => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    DateTime d => d.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
                    decimal m => m.ToString("G29", CultureInfo.InvariantCulture),
                    _ => Convert.ToString(v, CultureInfo.InvariantCulture),
                };
            }

            actual.Add(row);
        }

        var rows = expected.Cast<JObject>().Select(o => o.Properties().ToDictionary(p => p.Name, p => (string?)p.Value)).ToList();
        Assert.Equal(rows.Count, actual.Count);
        for (int i = 0; i < rows.Count; i++)
        {
            foreach ((string column, string? value) in rows[i])
            {
                Assert.True(value == actual[i][column], $"{sql[..40]}… baris {i} kolom {column}: diharapkan '{value}', didapat '{actual[i][column]}'");
            }
        }
    }
}
