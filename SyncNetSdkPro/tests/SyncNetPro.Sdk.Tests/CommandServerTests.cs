using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using SyncNetPro.Sdk.Nodes;
using SyncNetPro.Sdk.Tests.Infrastructure;
using SyncNetPro.Sdk.Tracing;
using SyncNetPro.Sdk.Transport;

namespace SyncNetPro.Sdk.Tests;

public class CommandServerTests
{
    private static async Task<(byte[] Frame, string Text)> SendCommandAsync(int port, string command)
    {
        using var tcp = new TcpClient();
        await tcp.ConnectAsync("127.0.0.1", port);
        NetworkStream stream = tcp.GetStream();
        await stream.WriteAsync(LengthPrefixCodec.Default.Encode(Encoding.UTF8.GetBytes(command)));

        byte[] header = new byte[2];
        await stream.ReadExactlyAsync(header).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        byte[] payload = new byte[(header[0] << 8) | header[1]];
        await stream.ReadExactlyAsync(payload);
        return ([.. header, .. payload], Encoding.UTF8.GetString(payload));
    }

    private static async Task<TestHost<ScriptedInterface>> StartAsync(FakeCore core) =>
        await TestHost<ScriptedInterface>.StartAsync([Messages.Node("BILLER", NodeCategory.BillerIssuer, core)]);

    private static int Port(TestHost<ScriptedInterface> app) => app.Runtime.CommandEndPoint!.Port;

    private static byte[] Golden(string file) => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Golden", file));

    [Theory]
    [InlineData("VERSION")]
    [InlineData("version")]
    [InlineData("  Version  ")]
    public async Task Version_is_case_insensitive(string command)
    {
        await using FakeCore core = await FakeCore.StartAsync();
        await using var app = await StartAsync(core);

        Assert.Equal("v9.9-test", (await SendCommandAsync(Port(app), command)).Text);
    }

    [Fact]
    public async Task Replies_are_framed_like_legacy_sdk()
    {
        await using FakeCore core = await FakeCore.StartAsync();
        await using var app = await StartAsync(core);

        Assert.Equal(Golden("command-ok.frame.bin"), (await SendCommandAsync(Port(app), "TRACE ON")).Frame);
        Assert.Equal(Golden("command-unknown.frame.bin"), (await SendCommandAsync(Port(app), "FOO")).Frame);
    }

    [Fact]
    public async Task Trace_commands_toggle_and_clear()
    {
        await using FakeCore core = await FakeCore.StartAsync();
        await using var app = await StartAsync(core);
        var trace = app.Services.GetRequiredService<ITraceWriter>();

        Assert.Equal("OK", (await SendCommandAsync(Port(app), "trace off")).Text);
        Assert.False(trace.IsEnabled);
        Assert.Equal("OK", (await SendCommandAsync(Port(app), "TRACE ON")).Text);
        Assert.True(trace.IsEnabled);
        Assert.Equal("OK", (await SendCommandAsync(Port(app), "TRACE CLEAR")).Text);
        Assert.Equal("Unknown command", (await SendCommandAsync(Port(app), "TRACE MAYBE")).Text);
    }

    [Fact]
    public async Task Network_commands_reach_handler()
    {
        await using FakeCore core = await FakeCore.StartAsync();
        await using var app = await StartAsync(core);

        Assert.Equal("OK", (await SendCommandAsync(Port(app), "SIGNON BILLER")).Text);
        Assert.Equal("OK", (await SendCommandAsync(Port(app), "echo BILLER")).Text);
        Assert.Equal("OK", (await SendCommandAsync(Port(app), "OTHER BILLER RESET KEY 01")).Text);
        Assert.Equal("OK", (await SendCommandAsync(Port(app), "KEYCHANGE UNKNOWN_NODE")).Text);

        NetworkCommandContext[] commands = [.. app.Handler.Commands];
        Assert.Equal([NetworkCommand.SignOn, NetworkCommand.Echo, NetworkCommand.Other, NetworkCommand.KeyChange], commands.Select(c => c.Command));
        Assert.Equal("BILLER", commands[0].NodeName);
        Assert.NotNull(commands[0].Node);
        Assert.Equal("RESET KEY 01", commands[2].Parameter);
        Assert.Null(commands[3].Node);
    }

    [Fact]
    public async Task Resync_reloads_configuration()
    {
        await using FakeCore core = await FakeCore.StartAsync();
        await using var app = await StartAsync(core);

        Assert.Equal("OK", (await SendCommandAsync(Port(app), "RESYNC")).Text);
        Assert.Equal(1, app.Handler.Reloads);
    }
}
