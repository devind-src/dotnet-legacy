using Microsoft.Extensions.DependencyInjection;
using SyncNetPro.Contracts;
using SyncNetPro.Sdk.Core;
using SyncNetPro.Sdk.Nodes;
using SyncNetPro.Sdk.Tests.Infrastructure;

namespace SyncNetPro.Sdk.Tests;

/// <summary>Perilaku kanal Core end-to-end: host nyata ↔ FakeCore lewat socket.</summary>
public class CoreChannelTests
{
    [Fact]
    public async Task Outbound_request_is_answered_through_handler()
    {
        await using FakeCore core = await FakeCore.StartAsync();
        await using var app = await TestHost<ScriptedInterface>.StartAsync([Messages.Node("BILLER", NodeCategory.BillerIssuer, core)]);
        app.Handler.OnRequest = ctx => Task.FromResult<CoreResponse?>(ctx.Request.ToResponse("00", "Approved"));
        await Wait.UntilAsync(() => core.SinkConnected, "kanal outbound terkoneksi");

        await core.SendToInterfaceAsync(Messages.Request());
        CoreResponse response = await core.ReceiveResponseAsync();

        Assert.Equal("00", response.ResponseCode);
        Assert.Equal("0210", response.MessageType);
        Assert.Equal("021", response.PosEntryMode);
        Assert.Equal("000123", response.TraceNumber);
        Assert.Equal(AuthorizedBy.External, response.AuthorizedBy);
    }

    [Fact]
    public async Task Default_handler_replies_not_supported()
    {
        await using FakeCore core = await FakeCore.StartAsync();
        await using var app = await TestHost<DefaultInterface>.StartAsync([Messages.Node("BILLER", NodeCategory.BillerIssuer, core)]);
        await Wait.UntilAsync(() => core.SinkConnected, "kanal outbound terkoneksi");

        await core.SendToInterfaceAsync(Messages.Request());
        CoreResponse response = await core.ReceiveResponseAsync();

        Assert.Equal(ResponseCodes.NotSupported, response.ResponseCode);
        Assert.Equal("Transaction is not supported", response.ResponseMessage);
        Assert.Equal(AuthorizedBy.Internal, response.AuthorizedBy);
    }

    [Fact]
    public async Task Handler_error_does_not_reply_and_does_not_stop_processing()
    {
        await using FakeCore core = await FakeCore.StartAsync();
        await using var app = await TestHost<ScriptedInterface>.StartAsync([Messages.Node("BILLER", NodeCategory.BillerIssuer, core)]);
        app.Handler.OnRequest = ctx => ctx.Request.TraceNumber == "000001"
            ? throw new InvalidOperationException("boom")
            : Task.FromResult<CoreResponse?>(ctx.Request.ToResponse("00"));
        await Wait.UntilAsync(() => core.SinkConnected, "kanal outbound terkoneksi");

        await core.SendToInterfaceAsync(Messages.Request("000001"));
        await core.SendToInterfaceAsync(Messages.Request("000002"));

        CoreResponse response = await core.ReceiveResponseAsync();
        Assert.Equal("000002", response.TraceNumber);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => core.ReceiveResponseAsync(300));
    }

    [Fact]
    public async Task Requests_are_processed_concurrently()
    {
        await using FakeCore core = await FakeCore.StartAsync();
        await using var app = await TestHost<ScriptedInterface>.StartAsync([Messages.Node("BILLER", NodeCategory.BillerIssuer, core)]);
        var release = new TaskCompletionSource();
        int started = 0;
        app.Handler.OnRequest = async ctx =>
        {
            Interlocked.Increment(ref started);
            await release.Task;
            return ctx.Request.ToResponse("00");
        };
        await Wait.UntilAsync(() => core.SinkConnected, "kanal outbound terkoneksi");

        for (int i = 1; i <= 5; i++) await core.SendToInterfaceAsync(Messages.Request($"00000{i}"));
        await Wait.UntilAsync(() => Volatile.Read(ref started) == 5, "5 request diproses bersamaan");
        release.SetResult();

        var traces = new HashSet<string?>();
        for (int i = 0; i < 5; i++) traces.Add((await core.ReceiveResponseAsync()).TraceNumber);
        Assert.Equal(5, traces.Count);
    }

    [Fact]
    public async Task Inbound_send_waits_for_correlated_core_response()
    {
        await using FakeCore core = await FakeCore.StartAsync();
        await using var app = await TestHost<DefaultInterface>.StartAsync([Messages.Node("CHANNEL", NodeCategory.Merchant, core)]);
        var client = app.Services.GetRequiredService<ICoreClient>();
        await Wait.UntilAsync(() => client.IsConnected("CHANNEL", CoreChannelDirection.Inbound), "kanal inbound terkoneksi");

        CoreResponse response = await client.SendAsync("CHANNEL", Messages.Request(),
            new CoreSendOptions { ConnectionName = "CONN_A", RemoteAddress = "10.1.2.3" });

        Assert.Equal("00", response.ResponseCode);
        Assert.True(core.SourceRequests.TryDequeue(out CoreRequest? sent));
        Assert.Equal("CONN_A", sent.PrivateData!.ConnectionName);
        Assert.Equal("10.1.2.3", sent.PrivateData.IpExternal);
        Assert.False(core.SinkConnected, "node MERCHANT tidak membuka kanal outbound");
    }

    [Fact]
    public async Task Inbound_send_times_out_when_core_is_silent()
    {
        await using FakeCore core = await FakeCore.StartAsync();
        core.SourceResponder = null;
        await using var app = await TestHost<DefaultInterface>.StartAsync([Messages.Node("CHANNEL", NodeCategory.Merchant, core)]);
        var client = app.Services.GetRequiredService<ICoreClient>();
        await Wait.UntilAsync(() => client.IsConnected("CHANNEL", CoreChannelDirection.Inbound), "kanal inbound terkoneksi");

        await Assert.ThrowsAsync<TimeoutException>(() =>
            client.SendAsync("CHANNEL", Messages.Request(), new CoreSendOptions { Timeout = TimeSpan.FromMilliseconds(200) }));
    }

    [Fact]
    public async Task Duplicate_pending_request_is_rejected()
    {
        await using FakeCore core = await FakeCore.StartAsync();
        core.SourceResponder = null;
        await using var app = await TestHost<DefaultInterface>.StartAsync([Messages.Node("CHANNEL", NodeCategory.Merchant, core)]);
        var client = app.Services.GetRequiredService<ICoreClient>();
        await Wait.UntilAsync(() => client.IsConnected("CHANNEL", CoreChannelDirection.Inbound), "kanal inbound terkoneksi");

        Task<CoreResponse> first = client.SendAsync("CHANNEL", Messages.Request(), new CoreSendOptions { Timeout = TimeSpan.FromSeconds(2) });
        await Assert.ThrowsAsync<DuplicateCoreRequestException>(() => client.SendAsync("CHANNEL", Messages.Request()));

        await core.SendSourceResponseAsync(Messages.Request().ToResponse("00"));
        Assert.Equal("00", (await first).ResponseCode);
    }

    [Fact]
    public async Task Inbound_send_is_rejected_for_unknown_node_or_outbound_only_node()
    {
        await using FakeCore core = await FakeCore.StartAsync();
        await using var app = await TestHost<DefaultInterface>.StartAsync([Messages.Node("BILLER", NodeCategory.BillerIssuer, core)]);
        var client = app.Services.GetRequiredService<ICoreClient>();

        await Assert.ThrowsAsync<CoreUnavailableException>(() => client.SendAsync("NOPE", Messages.Request()));
        await Assert.ThrowsAsync<CoreUnavailableException>(() => client.SendAsync("BILLER", Messages.Request()));
    }

    [Fact]
    public async Task Unmatched_core_response_goes_to_handler()
    {
        await using FakeCore core = await FakeCore.StartAsync();
        await using var app = await TestHost<ScriptedInterface>.StartAsync([Messages.Node("CHANNEL", NodeCategory.Merchant, core)]);
        await Wait.UntilAsync(() => core.SourceConnected, "kanal inbound terkoneksi");

        await core.SendSourceResponseAsync(Messages.Request("999999").ToResponse("68"));

        await Wait.UntilAsync(() => !app.Handler.Unmatched.IsEmpty, "respons tak dikenal diteruskan");
        Assert.True(app.Handler.Unmatched.TryPeek(out CoreResponse? late));
        Assert.Equal("999999", late.TraceNumber);
    }

    [Fact]
    public async Task Stop_closes_every_core_channel_including_merchant_only_nodes()
    {
        // Perbaikan B3: SDK lama tidak menutup kanal node MERCHANT saat berhenti.
        await using FakeCore merchantCore = await FakeCore.StartAsync();
        await using FakeCore bothCore = await FakeCore.StartAsync();
        var app = await TestHost<DefaultInterface>.StartAsync(
        [
            Messages.Node("CHANNEL", NodeCategory.Merchant, merchantCore),
            Messages.Node("SWITCH", NodeCategory.Both, bothCore),
        ]);
        await Wait.UntilAsync(() => merchantCore.SourceConnected && bothCore.SourceConnected && bothCore.SinkConnected, "semua kanal terkoneksi");

        await app.DisposeAsync();

        await Wait.UntilAsync(() => merchantCore.SourceDisconnects == 1, "kanal merchant ditutup");
        await Wait.UntilAsync(() => bothCore.SourceDisconnects == 1 && bothCore.SinkDisconnects == 1, "kanal node both ditutup");
    }

    [Fact]
    public async Task Stop_waits_for_in_flight_requests_to_reply()
    {
        await using FakeCore core = await FakeCore.StartAsync();
        var app = await TestHost<ScriptedInterface>.StartAsync([Messages.Node("BILLER", NodeCategory.BillerIssuer, core)]);
        ScriptedInterface handler = app.Handler;
        handler.OnRequest = async ctx =>
        {
            await Task.Delay(300);
            return ctx.Request.ToResponse("00");
        };
        await Wait.UntilAsync(() => core.SinkConnected, "kanal outbound terkoneksi");
        await core.SendToInterfaceAsync(Messages.Request());
        await Task.Delay(50);

        await app.DisposeAsync();

        Assert.Equal("00", (await core.ReceiveResponseAsync()).ResponseCode);
        Assert.Equal(1, handler.Stopping);
    }

    [Fact]
    public async Task Resync_opens_new_nodes_closes_removed_nodes_and_notifies_handler()
    {
        await using FakeCore first = await FakeCore.StartAsync();
        await using FakeCore second = await FakeCore.StartAsync();
        var source = new MutableNodeSource { Nodes = [Messages.Node("A", NodeCategory.BillerIssuer, first)] };
        await using var app = await TestHost<ScriptedInterface>.StartAsync([], services: s => s.AddSingleton<INodeConfigurationSource>(source));
        await Wait.UntilAsync(() => first.SinkConnected, "node A terkoneksi");

        source.Nodes = [Messages.Node("B", NodeCategory.BillerIssuer, second)];
        await app.Runtime.ReloadAsync();

        await Wait.UntilAsync(() => second.SinkConnected, "node B terkoneksi");
        await Wait.UntilAsync(() => first.SinkDisconnects == 1, "node A ditutup");
        Assert.Equal(1, app.Handler.Reloads);
        Assert.Equal(1, app.Handler.Started);
    }
}
