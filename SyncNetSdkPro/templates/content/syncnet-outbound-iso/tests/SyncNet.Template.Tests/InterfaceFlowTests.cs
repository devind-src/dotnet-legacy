using Microsoft.Extensions.DependencyInjection;
using SyncNetPro.Sdk.Nodes;
using SyncNetPro.Sdk.Remote;
using SyncNetPro.Sdk.Testing;

namespace SyncNet.Template.Tests;

/// <summary>
/// End-to-end tanpa Core: SimCore (in-process) mengirim transaksi ke interface, interface meneruskan ke biller tiruan
/// (stub ISO di <c>simcore/simcore.json</c>), lalu semua skenario <c>simcore/scenarios</c> diperiksa.
/// </summary>
public class InterfaceFlowTests
{
    [Fact]
    public async Task All_simcore_scenarios_pass()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        SimCoreOptions options = SimCoreOptions.Load(Path.Combine(AppContext.BaseDirectory, "simcore", "simcore.json"));
        UseRandomPorts(options);

        await using SimCore core = await SimCore.StartAsync(options, cancellationToken: ct);
        RemoteConnectionInfo biller = new()
        {
            Name = "TEMPLATE_NODE_TCP",
            NodeName = "TEMPLATE_NODE",
            Protocol = ConnectionProtocol.Tcp2ByteExcludeHeader,
            Role = ConnectionRole.Client,
            Host = "127.0.0.1",
            Port = core.RemoteStubs[0].Port,
        };
        await using var app = await SimInterfaceHost.StartAsync<BillerInterface>(core, [biller], services: s => s.AddBillerServices());
        await WaitUntilAsync(() => app.Services.GetRequiredService<IRemoteRegistry>().GetNode("TEMPLATE_NODE").IsConnected, ct);

        var runner = new ScenarioRunner(core);
        IReadOnlyList<ScenarioResult> results = await runner.RunAsync(ScenarioRunner.Load(Path.Combine(AppContext.BaseDirectory, "simcore", "scenarios")), ct);

        Assert.All(results, r => Assert.True(r.Passed, $"{r.Name}: {string.Join("; ", r.Failures)}"));
        Assert.Empty(core.ContractWarnings);
    }

    private static void UseRandomPorts(SimCoreOptions options)
    {
        options.LogServicesPort = 0;
        options.Interface = null;
        foreach (SimNodeOptions node in options.Nodes) node.PortIn = node.PortOut = 0;
        foreach (RemoteStubOptions stub in options.RemoteStubs) stub.Port = 0;
    }

    private static async Task WaitUntilAsync(Func<bool> condition, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        while (!condition()) await Task.Delay(20, timeout.Token);
    }
}
