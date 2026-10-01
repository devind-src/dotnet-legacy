using SyncNetPro.Sdk.Testing;

namespace SyncNet.Template.Tests;

/// <summary>
/// End-to-end tanpa Core: SimCore (in-process) menjalankan semua skenario <c>simcore/scenarios</c> terhadap interface.
/// Tambahkan skenario baru (JSON) setiap kali menambah transaksi.
/// </summary>
public class InterfaceFlowTests
{
    [Fact]
    public async Task All_simcore_scenarios_pass()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        SimCoreOptions options = SimCoreOptions.Load(Path.Combine(AppContext.BaseDirectory, "simcore", "simcore.json"));
        options.LogServicesPort = 0;
        options.Interface = null;
        foreach (SimNodeOptions node in options.Nodes) node.PortIn = node.PortOut = 0;

        await using SimCore core = await SimCore.StartAsync(options, cancellationToken: ct);
        await using var app = await SimInterfaceHost.StartAsync<MyInterface>(core);

        var runner = new ScenarioRunner(core);
        IReadOnlyList<ScenarioResult> results = await runner.RunAsync(ScenarioRunner.Load(Path.Combine(AppContext.BaseDirectory, "simcore", "scenarios")), ct);

        Assert.All(results, r => Assert.True(r.Passed, $"{r.Name}: {string.Join("; ", r.Failures)}"));
        Assert.Empty(core.ContractWarnings);
    }
}
