using System.CommandLine;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SyncNetPro.Sdk.Nodes;
using SyncNetPro.Sdk.Testing;
using SyncNetPro.SimCore;

using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss "; }).SetMinimumLevel(LogLevel.Information));
ILogger logger = loggerFactory.CreateLogger("SimCore");

var config = new Option<string>("--config", "-c") { Description = "File konfigurasi SimCore", DefaultValueFactory = _ => "simcore.json" };
var scenarios = new Option<string?>("--scenarios", "-s") { Description = "Folder/file skenario" };
var urls = new Option<string>("--urls") { Description = "Alamat Web UI/API", DefaultValueFactory = _ => "http://127.0.0.1:5080" };

// up: SimCore + Web UI sampai dihentikan
var up = new Command("up", "Menjalankan SimCore dan Web UI") { config, scenarios, urls };
up.SetAction(async (parse, ct) =>
{
    SimCoreOptions options = SimCoreOptions.Load(parse.GetValue(config)!);
    await using SyncNetPro.Sdk.Testing.SimCore core = await SyncNetPro.Sdk.Testing.SimCore.StartAsync(options, logger, ct);
    await using var web = SimCoreWebApp.Build(core, parse.GetValue(scenarios), parse.GetValue(urls)!);
    await web.StartAsync(ct);
    logger.LogInformation("Web UI: {Urls}  (Ctrl+C untuk berhenti)", string.Join(", ", web.Urls));
    foreach (SimNode node in core.Nodes)
    {
        logger.LogInformation("Node {Node}: PortOut(sink)={Sink} PortIn(source)={Source}", node.Name, node.SinkPort, node.SourcePort);
    }

    try
    {
        await Task.Delay(Timeout.Infinite, ct);
    }
    catch (OperationCanceledException)
    {
    }

    return 0;
});

// run: jalankan skenario sebagai regression test (tanpa UI)
var wait = new Option<int>("--wait") { Description = "Detik menunggu interface terkoneksi", DefaultValueFactory = _ => 60 };
var report = new Option<string?>("--report") { Description = "Tulis laporan JUnit XML" };
var run = new Command("run", "Menjalankan skenario lalu keluar (exit code 1 bila ada yang gagal)") { config, scenarios, wait, report };
run.SetAction(async (parse, ct) =>
{
    SimCoreOptions options = SimCoreOptions.Load(parse.GetValue(config)!);
    string path = parse.GetValue(scenarios) ?? throw new InvalidOperationException("--scenarios wajib diisi.");
    IReadOnlyList<SimScenario> list = ScenarioRunner.Load(path);
    await using SyncNetPro.Sdk.Testing.SimCore core = await SyncNetPro.Sdk.Testing.SimCore.StartAsync(options, logger, ct);

    foreach (string node in list.Where(s => s.Request is not null).Select(s => s.Node ?? core.Nodes.First().Name).Distinct(StringComparer.Ordinal))
    {
        logger.LogInformation("Menunggu interface terkoneksi ke {Node}...", node);
        await core.WaitForConnectionAsync(node, TimeSpan.FromSeconds(parse.GetValue(wait)), ct);
    }

    IReadOnlyList<ScenarioResult> results = await new ScenarioRunner(core).RunAsync(list, ct);
    foreach (ScenarioResult r in results)
    {
        Console.WriteLine($"{(r.Passed ? "PASS" : "FAIL")}  {r.Name} ({r.Elapsed.TotalMilliseconds:0} ms)");
        foreach (string f in r.Failures) Console.WriteLine($"      - {f}");
    }

    Console.WriteLine($"{results.Count(r => r.Passed)}/{results.Count} lulus");
    if (parse.GetValue(report) is { } file) ScenarioRunner.WriteJUnit(file, results);
    return results.All(r => r.Passed) ? 0 : 1;
});

// send: kirim ke instance 'up' yang sedang berjalan
var server = new Option<string>("--server") { Description = "Alamat SimCore yang berjalan", DefaultValueFactory = _ => "http://127.0.0.1:5080" };
var scenarioName = new Option<string>("--scenario") { Description = "Nama skenario (dari --scenarios instance 'up')", Required = true };
var send = new Command("send", "Menjalankan skenario pada SimCore yang sedang berjalan") { server, scenarioName };
send.SetAction(async (parse, ct) =>
{
    using var http = new HttpClient { BaseAddress = new Uri(parse.GetValue(server)!) };
    using var content = new StringContent(JsonConvert.SerializeObject(new { name = parse.GetValue(scenarioName) }), System.Text.Encoding.UTF8, "application/json");
    using HttpResponseMessage response = await http.PostAsync(new Uri("/api/scenarios/run", UriKind.Relative), content, ct);
    Console.WriteLine(await response.Content.ReadAsStringAsync(ct));
    return response.IsSuccessStatusCode ? 0 : 1;
});

// cmd: command port interface
var host = new Option<string>("--host") { Description = "Host command port interface", DefaultValueFactory = _ => "127.0.0.1" };
var port = new Option<int>("--port", "-p") { Description = "Port command interface", Required = true };
var text = new Argument<string[]>("command") { Description = "Perintah, mis. VERSION atau ECHO NODE", Arity = ArgumentArity.OneOrMore };
var cmd = new Command("cmd", "Mengirim perintah ke command port interface") { host, port, text };
cmd.SetAction(async (parse, ct) =>
{
    Console.WriteLine(await SimCommandClient.SendAsync(parse.GetValue(host)!, parse.GetValue(port), string.Join(' ', parse.GetValue(text)!), TimeSpan.FromSeconds(10), ct));
    return 0;
});

// init: contoh konfigurasi
var dir = new Option<string>("--dir") { Description = "Folder tujuan", DefaultValueFactory = _ => "simcore" };
var node = new Option<string>("--node") { Description = "Nama node", DefaultValueFactory = _ => "SAMPLE_BILLER" };
var init = new Command("init", "Membuat contoh simcore.json dan skenario") { dir, node };
init.SetAction(parse =>
{
    string folder = parse.GetValue(dir)!;
    string nodeName = parse.GetValue(node)!;
    Directory.CreateDirectory(Path.Combine(folder, "scenarios"));
    var options = new SimCoreOptions
    {
        AppName = "API Sample",
        LogServicesPort = 17009,
        Interface = new SimCommandTarget { Port = 17000 },
        Nodes = [new SimNodeOptions { Name = nodeName, Category = NodeCategory.BillerIssuer, PortIn = 17001, PortOut = 17002 }],
    };
    File.WriteAllText(Path.Combine(folder, "simcore.json"), options.ToJson());
    File.WriteAllText(Path.Combine(folder, "scenarios", "inquiry.json"), SampleScenarios.Inquiry(nodeName));
    File.WriteAllText(Path.Combine(folder, "scenarios", "version.json"), SampleScenarios.Version);
    Console.WriteLine($"Dibuat: {Path.Combine(folder, "simcore.json")}, {Path.Combine(folder, "scenarios")}");
    Console.WriteLine("Konfigurasi interface (appsettings.Development.json) yang cocok:");
    Console.WriteLine(SampleScenarios.InterfaceSettings(options));
    return 0;
});

var root = new RootCommand("SyncNet SimCore — simulator Core untuk pengembangan interface") { up, run, send, cmd, init };
return await root.Parse(args).InvokeAsync();
