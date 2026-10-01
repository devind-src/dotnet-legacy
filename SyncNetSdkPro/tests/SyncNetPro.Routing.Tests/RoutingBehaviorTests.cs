using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using SyncNetPro.Contracts;
using SyncNetPro.Sdk;
using SyncNetPro.Sdk.Nodes;
using SyncNetPro.Sdk.Testing;
using CoreSim = SyncNetPro.Sdk.Testing.SimCore;

namespace SyncNetPro.Routing.Tests;

/// <summary>Perilaku SDK baru di luar paritas: perbaikan bug SDK lama dan integrasi dengan host.</summary>
public class RoutingBehaviorTests
{
    [Fact]
    public async Task Price_reload_never_exposes_an_empty_book_to_running_requests()
    {
        // SDK lama mengosongkan dictionary di tempat saat RESYNC; request yang berjalan bisa melihat produk tanpa harga.
        var store = new SlowDataStore();
        var prices = new PriceBook(store);
        await prices.InitializeAsync(TestContext.Current.CancellationToken);

        using var stop = new CancellationTokenSource();
        int misses = 0;
        Task reader = Task.Run(() =>
        {
            while (!stop.IsCancellationRequested)
            {
                if (!prices.TryGetRankedSuppliers("TSEL10", 10000, out _)) Interlocked.Increment(ref misses);
            }
        }, TestContext.Current.CancellationToken);

        for (int i = 0; i < 20; i++) await prices.InitializeAsync(TestContext.Current.CancellationToken);
        await stop.CancelAsync();
        await reader;

        Assert.Equal(0, misses);
    }

    [Fact]
    public async Task Load_balance_uses_weights_of_healthy_candidates()
    {
        var store = new SlowDataStore { Delay = TimeSpan.Zero };
        store.Routes.Add(new ProductRouteEntry("PLN", 1, "A", 1, null, 0));
        store.Alternates.Add(new ProductRouteEntry("PLN", 2, "B", 2, null, 3));
        store.Alternates.Add(new ProductRouteEntry("PLN", 3, "C", 3, null, 1));
        var time = new TestClock(new DateTime(2026, 9, 28, 10, 0, 0, DateTimeKind.Unspecified));
        var health = new SupplierHealthTracker(new HealthStore(), time);
        var schedule = new RoutingSchedule(new ScheduleStore([]), time);
        var routing = new ProductRouting(store, health, schedule, random: new Random(42));
        await routing.InitializeAsync(TestContext.Current.CancellationToken);

        var picks = Enumerable.Range(0, 4000).Select(_ => routing.SelectNode("PLN", RoutingModes.LoadBalance).NodeName).GroupBy(n => n).ToDictionary(g => g.Key, g => g.Count());

        Assert.False(picks.ContainsKey("A")); // bobot 0
        Assert.InRange(picks["B"] / (double)picks["C"], 2.5, 3.5);
    }

    [Fact]
    public void Routing_context_is_built_from_core_request()
    {
        var request = new CoreRequest
        {
            TranType = TranType.Payment, ReceivingInstitutionId = "PLN", Amount = 125000, MerchantId = "M001", TerminalId = "T1",
            ReferenceNumber = "R1", TransactionDateTime = "0928100000", TraceNumber = "000001", OriginalData = null,
        };

        RoutingContext ctx = RoutingContext.From(request);

        Assert.Equal(("PLN", 125000L, "R1", "M001"), (ctx.ProductId, ctx.Denom, ctx.Refnum, ctx.MerchantId));
        Assert.Equal("PAY0928100000000001T1", SwitchKeys.Build(ctx.TranType, ctx.TransactionDateTime, ctx.TraceNumber, ctx.TerminalId));
        Assert.Null(SwitchKeys.ParseTransactionDate("0928100000"));
        Assert.Equal(new DateTime(2026, 9, 28, 10, 0, 0, DateTimeKind.Unspecified), SwitchKeys.ParseTransactionDate("PAY20260928100000000001", hasTranTypePrefix: true));
    }

    [Fact]
    public void Switch_key_uses_invariant_upper_case()
    {
        // SDK lama memakai ToUpper() yang bergantung culture (mis. tr-TR: 'i' → 'İ').
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            Assert.Equal("INQ0928100000000001TERMI1", SwitchKeys.Build("inq", "0928100000", "000001", "termi1"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    private sealed class SlowDataStore : IRoutingDataStore
    {
        public TimeSpan Delay { get; set; } = TimeSpan.FromMilliseconds(5);

        public List<ProductRouteEntry> Routes { get; } = [];

        public List<ProductRouteEntry> Alternates { get; } = [];

        public async Task<IReadOnlyList<SupplierPrice>> GetSupplierPricesAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(Delay, cancellationToken);
            return [new SupplierPrice("SUP_X", "TSEL10", 10000, 9800, 10500, 700, null, 1, true)];
        }

        public async Task<IReadOnlyList<MerchantPrice>> GetMerchantPricesAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(Delay, cancellationToken);
            return [];
        }

        public Task<IReadOnlyList<ProductRouteEntry>> GetProductRoutesAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ProductRouteEntry>>(Routes);

        public Task<IReadOnlyList<ProductRouteEntry>> GetAlternateProductRoutesAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ProductRouteEntry>>(Alternates);

        public Task<IReadOnlyList<MarginRouteEntry>> GetMarginRoutesAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<MarginRouteEntry>>([]);

        public Task<IReadOnlyList<FeeRule>> GetFeeRulesAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<FeeRule>>([]);
    }
}

/// <summary>Interface channel contoh dengan modul routing (dipakai test integrasi PostgreSQL).</summary>
public sealed class ChannelInterface(RoutingResolver routing, ProductFeeCalculator fees) : SyncNetInterface
{
    public override async Task<CoreResponse?> OnCoreRequestAsync(CoreRequestContext context, CancellationToken cancellationToken)
    {
        CoreRequest request = context.Request;
        (string mode, int? staticNode) = fees.GetRoutingMode(request.ReceivingInstitutionId!, request.MerchantId);
        RoutingDecision decision = await routing.ResolveProductAsync(RoutingContext.From(request), mode, staticNode, cancellationToken);
        CoreResponse response = request.ToResponse(decision.Rejected ? RoutingDecision.ScheduleRejectRc : "00");
        (response.AdditionalData ??= [])["node"] = decision.NodeName;
        return response;
    }
}

public sealed class RoutingHostTests : IAsyncLifetime
{
    private static readonly string? ConnectionString = Environment.GetEnvironmentVariable("SYNCNET_TEST_PG");
    private readonly string _schema = "routing_host_" + Guid.NewGuid().ToString("N")[..12];

    public async ValueTask InitializeAsync()
    {
        if (string.IsNullOrEmpty(ConnectionString)) return;
        await using var admin = new NpgsqlConnection(ConnectionString);
        await admin.OpenAsync();
        await using var cmd = new NpgsqlCommand($"CREATE SCHEMA {_schema}; SET search_path TO {_schema};"
            + await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Sql", "schema.sql"))
            + await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Sql", "seed.sql")), admin);
        await cmd.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (string.IsNullOrEmpty(ConnectionString)) return;
        NpgsqlConnection.ClearAllPools();
        await using var admin = new NpgsqlConnection(ConnectionString);
        await admin.OpenAsync();
        await using var cmd = new NpgsqlCommand($"DROP SCHEMA IF EXISTS {_schema} CASCADE", admin);
        await cmd.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task Interface_with_routing_module_loads_data_at_start_and_routes_requests()
    {
        Assert.SkipWhen(string.IsNullOrEmpty(ConnectionString), "Set SYNCNET_TEST_PG untuk menjalankan test PostgreSQL");
        string cs = new NpgsqlConnectionStringBuilder(ConnectionString) { SearchPath = _schema }.ConnectionString;
        await using CoreSim core = await CoreSim.StartAsync(new SimCoreOptions { Nodes = [new SimNodeOptions { Name = "CHANNEL", Category = NodeCategory.BillerIssuer }] });
        await using var app = await SimInterfaceHost.StartAsync<ChannelInterface>(core, services: s => s.AddSyncNetRouting(o => o.ConnectionString = cs));

        SimResult result = await core.SendAsync("CHANNEL", new CoreRequest
        {
            MessageType = "0200", TranType = TranType.Inquiry, ReceivingInstitutionId = "PDAM", MerchantId = "M001", TerminalId = "TERM01",
            ReferenceNumber = "H1", TransactionDateTime = "0928100000", TraceNumber = "000100",
        });

        // PDAM untuk M001 = STATIC ke BILLER_C; di jam nyata C bisa buka/tutup (aturan 09:00-12:00), keduanya valid.
        Assert.Equal(SimOutcome.Responded, result.Outcome);
        Assert.Contains(result.Response!.ResponseCode, new[] { "00", RoutingDecision.ScheduleRejectRc });
        Assert.Equal("BILLER_C", result.Response.AdditionalData?["node"]?.ToString());
        Assert.True(app.Services.GetRequiredService<RoutingResolver>().HasProductRoute("PLN"));
    }
}
