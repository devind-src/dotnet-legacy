// Golden resolver routing end-to-end: SyncNet.Routing + SyncNet.Fees (SDK lama) dijalankan terhadap PostgreSQL yang diisi
// tests/SyncNetPro.Routing.Tests/Sql/{schema,seed}.sql. Butuh SYNCNET_TEST_PG; tanpa itu file golden yang ada dibiarkan.
using System.Data;
using System.Text;
using Newtonsoft.Json;
using Npgsql;
using SyncNet.Common;
using SyncNet.DbRepository;
using SyncNet.Fees;
using SyncNet.Routing;
using SyncNet.Routing.Commitment;
using SyncNet.Routing.Failover;
using SyncNet.Routing.Schedule;

internal static class RoutingDbGolden
{
    public static readonly DateTime Now = new(2026, 9, 28, 10, 0, 0);

    public sealed record Step(string Op, string TranType = null, string Product = null, long Denom = 0, string Merchant = null, string SubMerchant = null,
        string Terminal = "TERM01", string Refnum = null, string DateTime = "0928100000", string Trace = null, string OriginalData = null,
        string Supplier = null, string RoutingType = null, string Rc = null, int? Latency = null, decimal Amount = 0, int? Sharing = null);

    public static IReadOnlyList<Step> Steps { get; } =
    [
        // bill payment dynamic (BEST_PRICE dari baris CA M001): C tutup jadwal, B SUSPECT -> A
        new("mode", Product: "PLN", Merchant: "M001"),
        new("product", "INQ", "PLN", 100000, "M001", Refnum: "R1", Trace: "000001"),
        new("product", "PAY", "PLN", 100000, "M001", Refnum: "R1", Trace: "000002"),
        new("record", "PAY", "PLN", 100000, Trace: "000002", Supplier: "BILLER_A", RoutingType: "PRODUCT", Rc: "00", Latency: 800, Amount: 100000,
            DateTime: "20260928100000"),
        // M002 ikut default (PRIORITY): A kuota harian habis, B diblokir -> kandidat pertama yang buka
        new("mode", Product: "PLN", Merchant: "M002"),
        new("mode", Product: "PLN", Merchant: "M001", SubMerchant: "S01"),
        new("product", "INQ", "PLN", 50000, "M002", Refnum: "R2", Trace: "000003"),
        // advice/reversal: via switch key PAY asal, refnum, lalu dest_node Core
        new("product", "ADV", "PLN", 100000, "M001", Refnum: "R1", Trace: "000004", OriginalData: "PAY0928100000000002"),
        new("product", "REV", "PLN", 50000, "M002", Refnum: "R2", Trace: "000005", OriginalData: "PAY0928100000999999"),
        new("product", "REV", "PLN", 50000, "M009", Refnum: "R9", Trace: "000006", OriginalData: "PAY0928093000000777"),
        // STATIC: biller pilihan buka (dicatat karena punya aturan), tanpa pilihan (Core), pilihan tutup (ditolak), payment siklus lama
        new("mode", Product: "BPJS", Merchant: "M001"),
        new("product", "INQ", "BPJS", 75000, "M001", Refnum: "R3", Trace: "000007"),
        new("mode", Product: "PDAM", Merchant: "M005"),
        new("product", "INQ", "PDAM", 40000, "M005", Refnum: "R4", Trace: "000008"),
        new("mode", Product: "PDAM", Merchant: "M001"),
        new("product", "INQ", "PDAM", 40000, "M001", Refnum: "R5", Trace: "000009"),
        new("product", "PAY", "PDAM", 40000, "M001", Refnum: "R5", Trace: "000010"),
        new("product", "ADV", "PDAM", 40000, "M001", Refnum: "R5", Trace: "000011", OriginalData: "PAY0928100000000010"),
        // topup
        new("margin", "INQ", "TSEL10", 10000, "M001", Refnum: "R10", Trace: "000020"),
        new("margin-fee", Product: "TSEL10", Denom: 10000, Merchant: "M001", Supplier: "SUP_X"),
        new("margin-fee", Product: "TSEL10", Denom: 10000, Merchant: "M002", Supplier: "SUP_Z"),
        new("margin-fee", Product: "TSEL10", Denom: 10000, Merchant: "M002", Supplier: "SUP_W"),
        new("margin", "PAY", "TSEL10", 10000, "M001", Refnum: "R11", Trace: "000021"),
        new("margin", "PAY", "XL10", 10000, "M001", Refnum: "R12", Trace: "000022"),
        new("margin", "INQ", "ISAT10", 10000, "M001", Refnum: "R13", Trace: "000023"),
        new("margin", "INQ", "ISAT10", 20000, "M001", Refnum: "R14", Trace: "000024"),
        new("static", Product: "TSEL10", Denom: 10000),
        new("static", Product: "ISAT10", Denom: 10000),
        // health check via RecordResult
        new("record", "INQ", "TSEL10", 10000, Trace: "000020", Supplier: "SUP_X", RoutingType: "MARGIN", Rc: "91", Amount: 10000),
        new("record", "PAY", "ISAT10", 10000, Trace: "000030", Supplier: "SUP_Z", RoutingType: "MARGIN", Rc: "68", Amount: 10000),
        new("record", "PAY", "ISAT10", 10000, Trace: "000031", Supplier: "SUP_Z", RoutingType: "MARGIN", Rc: "68", Amount: 10000),
        new("margin", "INQ", "ISAT10", 10000, "M001", Refnum: "R15", Trace: "000032"),
        new("record", "PAY", "PDAM", 40000, Trace: "000010", Supplier: "BILLER_C", RoutingType: "PRODUCT", Rc: "05", Latency: 4000, Amount: 40000,
            DateTime: "20260928100000"),
        new("volume", "PAY", "PLN", Supplier: "BILLER_A", RoutingType: "PRODUCT", Rc: "0000", Amount: 20000, Trace: "000040", DateTime: "20260928100000"),
        // fee
        new("fees", Product: "PLN", Merchant: "M001", Amount: 100000, Sharing: 3000),
        new("fees", Product: "PLN", Merchant: "M001", SubMerchant: "S01", Amount: 100000),
        new("fees", Product: "PLN", Merchant: "M777", Amount: 100000),
        new("fees", Product: "PDAM", Merchant: "M005", Amount: 200000),
        new("fees", Product: "PDAM", Merchant: "M001", Amount: 123456),
        new("fees", Product: "UNKNOWN", Merchant: "M001", Amount: 1000),
        new("flush"),
    ];

    public static void Write(string outDir)
    {
        string pg = Environment.GetEnvironmentVariable("SYNCNET_TEST_PG");
        if (string.IsNullOrEmpty(pg))
        {
            Console.WriteLine("SYNCNET_TEST_PG kosong: golden resolver routing tidak dibuat ulang");
            return;
        }

        string sqlDir = Path.GetFullPath(Path.Combine(outDir, "..", "Sql"));
        const string schema = "routing_golden";
        using (var admin = new NpgsqlConnection(pg))
        {
            admin.Open();
            Exec(admin, $"DROP SCHEMA IF EXISTS {schema} CASCADE; CREATE SCHEMA {schema}; SET search_path TO {schema};"
                + File.ReadAllText(Path.Combine(sqlDir, "schema.sql")) + File.ReadAllText(Path.Combine(sqlDir, "seed.sql")));
        }

        // SDK lama membaca connection string statis; search path diarahkan ke skema golden.
        SdkConfig.ConnectionString = new NpgsqlConnectionStringBuilder(pg) { SearchPath = schema }.ConnectionString;
        SdkConfig.LogDir = Path.Combine(Path.GetTempPath(), "syncnet-golden-logs");
        SdkConfig.TraceDir = SdkConfig.LogDir;

        DateTime clock = Now;
        var db = new DbMgr();
        var supplierStatus = new SupplierStatusRepository(db, () => clock);
        var schedule = new RoutingScheduleRepository(db, () => clock);
        var commitment = new CommitmentRepository(db, () => clock, 0);
        var prices = new PriceRepository();
        var fees = new BillPaymentFeeCalculator();
        var margin = new MarginCalculator(prices);
        var resolver = new RoutingResolver(new StaticRoutingStrategy(), new MarginRoutingStrategy(prices, supplierStatus, schedule, commitment),
            new ProductRoutingStrategy(supplierStatus, schedule, commitment), supplierStatus, schedule, commitment);

        fees.Initialize().GetAwaiter().GetResult();
        prices.Initialize().GetAwaiter().GetResult();
        resolver.Initialize().GetAwaiter().GetResult();

        var results = new List<object>();
        foreach (Step s in Steps)
        {
            var ctx = new RoutingContext
            {
                TranType = s.TranType, ProductId = s.Product, Denom = s.Denom, MerchantId = s.Merchant, TerminalId = s.Terminal, Refnum = s.Refnum,
                DateTimeTran = s.DateTime, TraceNumber = s.Trace, OriginalData = s.OriginalData,
            };
            object result = s.Op switch
            {
                "mode" => Mode(fees, s),
                "product" => Decision(resolver.ResolveProductAsync(ctx, fees.GetRoutingMode(s.Product, s.Merchant).Mode, fees.GetRoutingMode(s.Product, s.Merchant).StaticNodeId).GetAwaiter().GetResult()),
                "margin" => Decision(resolver.ResolveMarginAsync(ctx).GetAwaiter().GetResult()),
                "static" => resolver.ResolveStaticSupplierAsync(s.Product, s.Denom).GetAwaiter().GetResult(),
                "margin-fee" => FeesOf(margin.Calculate(s.Merchant, s.Product, s.Denom, s.Supplier)),
                "fees" => FeesOf(fees.GetFees(s.Merchant, s.Product, s.Amount, s.Sharing, s.SubMerchant).GetAwaiter().GetResult()),
                "record" => Record(resolver, s),
                "volume" => RecordVolume(resolver, s),
                "flush" => Flush(resolver),
                _ => throw new InvalidOperationException(s.Op),
            };
            results.Add(result);
        }

        object tables;
        using (var conn = new NpgsqlConnection(SdkConfig.ConnectionString))
        {
            conn.Open();
            tables = new
            {
                tranMap = Rows(conn, "SELECT merchant_id,terminal_id,refnum,routing_type,inst_id,denom,node_name,inquiry_switch_key,switch_key FROM sw_routes_tran_map ORDER BY id"),
                failoverLog = Rows(conn, "SELECT routing_type,inst_id,denom,trace_number,from_supplier_id,to_supplier_id,rc_code,reason,latency_ms,schedule_id FROM sw_routes_failover_log ORDER BY id"),
                supplierStatus = Rows(conn, "SELECT supplier_id,status,last_rc_code,consecutive_suspect_count,consecutive_failed_count,consecutive_pending_count,consecutive_latency_count,block_reason,last_latency_ms,retry_count,last_tran_dt,blocked_since,blocked_until,updated_by,updated_dt FROM sw_routes_supplier_status ORDER BY supplier_id"),
                volume = Rows(conn, "SELECT volume_date,routing_type,node_name,inst_id,tran_count,tran_amount FROM sw_routes_volume ORDER BY volume_date,routing_type,node_name,inst_id"),
                commitmentLog = Rows(conn, "SELECT commitment_id,rule_type,routing_type,node_name,inst_id,metric,period_type,period_start,event,volume_value,threshold_value FROM sw_routes_commitment_log ORDER BY id"),
            };
        }

        using (var admin = new NpgsqlConnection(pg))
        {
            admin.Open();
            Exec(admin, $"DROP SCHEMA IF EXISTS {schema} CASCADE");
        }

        var settings = new JsonSerializerSettings { Formatting = Formatting.Indented, DateFormatString = "yyyy-MM-ddTHH:mm:ss" };
        File.WriteAllText(Path.Combine(outDir, "resolver.json"), JsonConvert.SerializeObject(new { now = Now, steps = Steps, results, tables }, settings), new UTF8Encoding(false));
        Console.WriteLine($"{Steps.Count} langkah resolver routing (PostgreSQL) ditulis ke {outDir}");
    }

    private static object Mode(BillPaymentFeeCalculator fees, Step s)
    {
        var (mode, node) = fees.GetRoutingMode(s.Product, s.Merchant, s.SubMerchant);
        return new { mode, staticNodeId = node };
    }

    private static object Decision(RoutingDecision d) => new { d.Apply, d.NodeName, d.Rejected, d.ScheduleId };

    private static object FeesOf(SyncNet.Message.Fees f) =>
        new { f.total_fee, f.acquirer_fee, f.merchant_fee, f.issuer_fee, f.biller_fee, f.switch_fee, f.submerchant_fee };

    private static object Record(RoutingResolver resolver, Step s)
    {
        string key = s.TranType == "PAY" ? StickyRouteResolver.BuildSwitchKey(s.TranType, s.DateTime, s.Trace, s.Terminal) : null;
        resolver.RecordResultAsync(s.Supplier, s.RoutingType, s.TranType, s.Rc, s.Product, s.Denom, s.Trace, s.Latency, s.Amount, key, null,
            CommitmentRepository.ParseTranDate(s.DateTime)).GetAwaiter().GetResult();
        return "ok";
    }

    private static object RecordVolume(RoutingResolver resolver, Step s)
    {
        resolver.RecordVolume(s.Supplier, s.RoutingType, s.TranType, s.Rc, s.Product, s.Amount,
            StickyRouteResolver.BuildSwitchKey(s.TranType, s.DateTime, s.Trace, s.Terminal), null, CommitmentRepository.ParseTranDate(s.DateTime));
        return "ok";
    }

    private static object Flush(RoutingResolver resolver)
    {
        resolver.FlushVolumeAsync().GetAwaiter().GetResult();
        return "ok";
    }

    private static void Exec(NpgsqlConnection conn, string sql)
    {
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.ExecuteNonQuery();
    }

    private static List<Dictionary<string, object>> Rows(NpgsqlConnection conn, string sql)
    {
        using var cmd = new NpgsqlCommand(sql, conn);
        using var reader = cmd.ExecuteReader();
        var rows = new List<Dictionary<string, object>>();
        while (reader.Read())
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                object v = reader.IsDBNull(i) ? null : reader.GetValue(i);
                row[reader.GetName(i)] = v is null ? null : v is DateOnly day ? day.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                    : v is DateTime d ? d.ToString("yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) : v is decimal m ? m.ToString("G29", System.Globalization.CultureInfo.InvariantCulture) : Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture);
            }

            rows.Add(row);
        }

        return rows;
    }
}
