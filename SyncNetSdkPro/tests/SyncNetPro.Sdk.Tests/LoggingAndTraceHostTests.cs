using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using SyncNetPro.Contracts;
using SyncNetPro.Sdk.Nodes;
using SyncNetPro.Sdk.Tests.Infrastructure;
using SyncNetPro.Sdk.Tracing;
using SyncNetPro.Sdk.Transport;

namespace SyncNetPro.Sdk.Tests;

public class LoggingAndTraceHostTests
{
    [Fact]
    public async Task Trace_goes_to_log_services_host_from_configuration()
    {
        // Perbaikan B5: host Log Services dari konfigurasi dipakai ("localhost", bukan hard-coded 127.0.0.1).
        var received = new ConcurrentQueue<JObject>();
        await using var logServices = new TcpFrameServer(new IPEndPoint(IPAddress.Loopback, 0), LengthPrefixCodec.Default, new TcpFrameServerOptions(), NullLogger.Instance)
        {
            FrameReceived = (_, payload) => { received.Enqueue(JObject.Parse(Encoding.UTF8.GetString(payload))); return Task.CompletedTask; },
        };
        await logServices.StartAsync();

        await using FakeCore core = await FakeCore.StartAsync();
        var source = new MutableNodeSource
        {
            Nodes = [Messages.Node("BILLER", NodeCategory.BillerIssuer, core)],
            LogServices = new DnsEndPoint("localhost", logServices.LocalEndPoint.Port),
        };
        await using var app = await TestHost<ScriptedInterface>.StartAsync([], services: s => s.AddSingleton<INodeConfigurationSource>(source));
        app.Handler.OnRequest = ctx =>
        {
            ctx.Trace.Message(ctx.Node.Name, TraceDirection.Outgoing, "0200", "ISO DUMP", "10.0.0.9:7000");
            return Task.FromResult<CoreResponse?>(ctx.Request.ToResponse("00"));
        };
        await Wait.UntilAsync(() => core.SinkConnected && logServices.HasConnections, "kanal Core & Log Services terkoneksi");

        await core.SendToInterfaceAsync(Messages.Request());
        await core.ReceiveResponseAsync();

        await Wait.UntilAsync(() => received.Any(r => (string?)r["LogType"] == "transaction"), "trace transaksi diterima Log Services");
        JObject trace = received.First(r => (string?)r["LogType"] == "transaction");
        Assert.Equal("Test App", (string?)trace["AppName"]);
        Assert.Equal("BILLER", (string?)trace["FileName"]);
        Assert.Equal("<0200> Message to BILLER 10.0.0.9:7000", (string?)trace["Title"]);
        Assert.Equal("ISO DUMP", (string?)trace["Detail"]);
        Assert.Equal(["Datetime", "AppName", "FileName", "LogType", "Title", "Detail"], trace.Properties().Select(p => p.Name));
    }

    [Fact]
    public async Task Without_log_services_trace_and_log_go_to_files()
    {
        await using FakeCore core = await FakeCore.StartAsync();
        var app = await TestHost<ScriptedInterface>.StartAsync([Messages.Node("BILLER", NodeCategory.BillerIssuer, core)]);
        string home = app.Home;
        try
        {
            app.Handler.OnRequest = ctx =>
            {
                ctx.Trace.Message(ctx.Node.Name, TraceDirection.Incoming, "0210", "RSP", "10.0.0.9:7000");
                ctx.Logger.LogWarning("peringatan dari handler");
                return Task.FromResult<CoreResponse?>(ctx.Request.ToResponse("00"));
            };
            await Wait.UntilAsync(() => core.SinkConnected, "kanal outbound terkoneksi");
            await core.SendToInterfaceAsync(Messages.Request());
            await core.ReceiveResponseAsync();

            string traceDir = Path.Combine(home, "Traces", "test-app");
            await Wait.UntilAsync(() => Directory.Exists(traceDir) && Directory.GetFiles(traceDir, "biller_*.log").Length == 1, "file trace node");
            string traceText = File.ReadAllText(Directory.GetFiles(traceDir, "biller_*.log")[0]);
            Assert.Contains("<0210> Message from BILLER 10.0.0.9:7000", traceText, StringComparison.Ordinal);

            string logDir = Path.Combine(home, "Logs");
            await Wait.UntilAsync(() => Directory.Exists(logDir) && Directory.GetFiles(logDir, "test-app_*.log").Length == 1, "file log aplikasi");
            await Wait.UntilAsync(() => File.ReadAllText(Directory.GetFiles(logDir, "test-app_*.log")[0]).Contains("WARN peringatan dari handler", StringComparison.Ordinal), "log handler tertulis");
        }
        finally
        {
            await app.DisposeAsync();
        }
    }
}
