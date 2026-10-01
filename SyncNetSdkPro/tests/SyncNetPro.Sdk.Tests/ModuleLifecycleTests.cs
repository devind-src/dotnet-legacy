using Microsoft.Extensions.DependencyInjection;
using SyncNetPro.Sdk.Core;
using SyncNetPro.Sdk.Hosting;
using SyncNetPro.Sdk.Nodes;
using SyncNetPro.Sdk.Tests.Infrastructure;

namespace SyncNetPro.Sdk.Tests;

public class ModuleLifecycleTests
{
    /// <summary>Mencatat urutan panggilan beserta status kanal Core saat itu.</summary>
    private sealed class RecordingModule(IServiceProvider services) : ISyncNetModule
    {
        public List<string> Calls { get; } = [];

        public Task StartAsync(CancellationToken cancellationToken) => Record("start");

        public Task ReloadAsync(CancellationToken cancellationToken) => Record("reload");

        public Task StopAsync(CancellationToken cancellationToken) => Record("stop");

        private Task Record(string call)
        {
            bool connected = services.GetRequiredService<ICoreClient>().IsConnected("BILLER", CoreChannelDirection.Outbound);
            lock (Calls) Calls.Add($"{call}:{(connected ? "open" : "closed")}");
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Modules_start_before_channels_open_reload_on_resync_and_stop_after_channels_close()
    {
        await using FakeCore core = await FakeCore.StartAsync();
        RecordingModule? module = null;
        var app = await TestHost<DefaultInterface>.StartAsync([Messages.Node("BILLER", NodeCategory.BillerIssuer, core)],
            services: s => s.AddSingleton<ISyncNetModule>(sp => module = new RecordingModule(sp)));

        await Wait.UntilAsync(() => core.SinkConnected, "kanal outbound terkoneksi");
        await app.Runtime.ReloadAsync(TestContext.Current.CancellationToken);
        await app.StopAsync();
        await app.DisposeAsync();

        Assert.NotNull(module);
        Assert.Equal(3, module.Calls.Count);
        Assert.Equal("start:closed", module.Calls[0]);
        Assert.StartsWith("reload:", module.Calls[1], StringComparison.Ordinal);
        Assert.Equal("stop:closed", module.Calls[2]);
    }
}
