using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Newtonsoft.Json.Linq;
using SyncNetPro.Sdk.Nodes;
using SyncNetPro.Sdk.Testing;
using CoreSim = SyncNetPro.Sdk.Testing.SimCore;

namespace SyncNetPro.SimCore.Tests;

public class WebApiTests
{
    [Fact]
    public async Task Api_serves_ui_status_send_command_and_events()
    {
        await using CoreSim core = await CoreSim.StartAsync(Samples.Options());
        await using var app = await SimInterfaceHost.StartAsync<BillerInterface>(core, configure: o => o.Version = "v5");
        await using WebApplication web = SimCoreWebApp.Build(core, null, "http://127.0.0.1:0");
        await web.StartAsync();
        using var http = new HttpClient { BaseAddress = new Uri(web.Urls.First()) };

        string html = await http.GetStringAsync("/");
        Assert.Contains("SyncNet SimCore", html, StringComparison.Ordinal);

        JObject status = JObject.Parse(await http.GetStringAsync("/api/status"));
        Assert.True((bool)status["nodes"]![0]!["sinkConnected"]!);

        using var events = await http.GetStreamAsync("/api/events");
        using var reader = new StreamReader(events);
        Assert.Equal(": connected", await reader.ReadLineAsync());

        using var send = new StringContent("""{ "node": "BILLER", "request": { "msgtype": "0200", "tran_type": "INQ", "trace_number": "{{stan}}", "datetime_tran": "{{now}}", "terminal_id": "T1" } }""", Encoding.UTF8, "application/json");
        JObject result = JObject.Parse(await (await http.PostAsync("/api/send", send)).Content.ReadAsStringAsync());
        Assert.Equal("Responded", (string?)result["outcome"]);
        Assert.Equal("00", (string?)result["response"]!["resp_code"]);

        // Event pertama bisa berupa trace; tunggu event pesan untuk node BILLER.
        string? line;
        do { line = await reader.ReadLineAsync(); }
        while (line is not null && !(line.StartsWith("data:", StringComparison.Ordinal) && line.Contains("\"node\":\"BILLER\"", StringComparison.Ordinal)));
        Assert.NotNull(line);

        using var cmd = new StringContent("""{ "text": "VERSION" }""", Encoding.UTF8, "application/json");
        JObject reply = JObject.Parse(await (await http.PostAsync("/api/command", cmd)).Content.ReadAsStringAsync());
        Assert.Equal("v5", (string?)reply["reply"]);
    }
}

/// <summary>Memakai port tetap untuk proses terpisah → dijalankan tanpa paralel agar tidak bentrok dengan port acak test lain.</summary>
[CollectionDefinition(nameof(CliTests), DisableParallelization = true)]
public sealed class CliSerialGroup;

[Collection(nameof(CliTests))]
public class CliTests
{
    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static async Task<(int Exit, string Output)> RunToolAsync(string workDir, params string[] args)
    {
        string dll = Path.Combine(AppContext.BaseDirectory, "SyncNetPro.SimCore.dll");
        var info = new ProcessStartInfo("dotnet") { WorkingDirectory = workDir, RedirectStandardOutput = true, RedirectStandardError = true };
        info.ArgumentList.Add(dll);
        foreach (string a in args) info.ArgumentList.Add(a);
        using Process process = Process.Start(info)!;
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(60));
        return (process.ExitCode, await output + await error);
    }

    [Fact]
    public async Task Init_then_run_scenarios_against_a_real_interface()
    {
        string dir = Path.Combine(Path.GetTempPath(), "cli-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);

        (int initExit, string initOutput) = await RunToolAsync(dir, "init", "--dir", "simcore", "--node", "BILLER");
        Assert.Equal(0, initExit);
        Assert.Contains("NodeSource", initOutput, StringComparison.Ordinal);

        // Sesuaikan port agar tidak bentrok antar test, lalu jalankan interface yang tersambung ke port tersebut.
        string config = Path.Combine(dir, "simcore", "simcore.json");
        SimCoreOptions options = SimCoreOptions.Load(config);
        options.Nodes[0].PortIn = FreePort();
        options.Nodes[0].PortOut = FreePort();
        options.LogServicesPort = FreePort();
        options.Interface = new SimCommandTarget { Port = FreePort() };
        File.WriteAllText(config, options.ToJson());

        Microsoft.Extensions.Hosting.HostApplicationBuilder builder = Microsoft.Extensions.Hosting.Host.CreateEmptyApplicationBuilder(new());
        builder.AddSyncNetInterfaceForCli(options, dir);
        using Microsoft.Extensions.Hosting.IHost host = builder.Build();
        await host.StartAsync();

        (int runExit, string runOutput) = await RunToolAsync(dir, "run", "--config", config, "--scenarios", Path.Combine(dir, "simcore", "scenarios"),
            "--wait", "20", "--report", Path.Combine(dir, "junit.xml"));

        await host.StopAsync();
        Assert.True(runExit == 0, runOutput);
        Assert.Contains("PASS  inquiry-sukses", runOutput, StringComparison.Ordinal);
        Assert.Contains("PASS  version", runOutput, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(dir, "junit.xml")));
    }
}

internal static class CliHostExtensions
{
    public static void AddSyncNetInterfaceForCli(this Microsoft.Extensions.Hosting.HostApplicationBuilder builder, SimCoreOptions options, string home) =>
        SyncNetPro.Sdk.SyncNetServiceCollectionExtensions.AddSyncNetInterface<BillerInterface>(builder, o =>
        {
            o.AppName = options.AppName;
            o.Version = "1.0.0";
            o.Home = home;
            o.NodeSource = SyncNetPro.Sdk.NodeSourceKind.Json;
            o.Core.ReconnectDelay = TimeSpan.FromMilliseconds(200);
            o.Command.BindAddress = "127.0.0.1";
            o.Command.Port = options.Interface!.Port;
            o.Trace.LogServicesHost = "127.0.0.1";
            o.Trace.LogServicesPort = options.LogServicesPort;
            o.Nodes = [new NodeInfo { Name = options.Nodes[0].Name, Category = options.Nodes[0].Category, PortIn = options.Nodes[0].PortIn, PortOut = options.Nodes[0].PortOut }];
        });
}
