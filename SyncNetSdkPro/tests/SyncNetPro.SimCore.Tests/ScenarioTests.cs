using System.Xml.Linq;
using SyncNetPro.Sdk.Testing;
using CoreSim = SyncNetPro.Sdk.Testing.SimCore;

namespace SyncNetPro.SimCore.Tests;

public class ScenarioTests
{
    private static string WriteScenarios(string json)
    {
        string dir = Path.Combine(Path.GetTempPath(), "scn-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "a.json"), json);
        return dir;
    }

    [Fact]
    public void Placeholders_are_expanded()
    {
        Assert.Equal("000042", ScenarioRunner.Expand("{{stan}}", "000042"));
        Assert.Matches("^[0-9]{10}$", ScenarioRunner.Expand("{{now:MMddHHmmss}}", "000042"));
        Assert.Matches("^[0-9]{6}000042$", ScenarioRunner.Expand("{{rrn}}", "000042"));
        Assert.Matches("^[0-9]{4}$", ScenarioRunner.Expand("{{random:4}}", "1"));
        Assert.Equal("x-{{unknown}}", ScenarioRunner.Expand("x-{{unknown}}", "1"));
    }

    [Fact]
    public async Task Runs_request_and_command_scenarios_with_expectations_and_junit()
    {
        string dir = WriteScenarios("""
            [
              { "name": "inq-ok", "node": "BILLER",
                "request": { "msgtype": "0200", "tran_type": "INQ", "trace_number": "{{stan}}", "datetime_tran": "{{now}}", "terminal_id": "T1" },
                "expect": { "resp_code": "00", "within_ms": 5000, "fields": { "additional_data.customer_name": "BUDI", "msgtype": "0210" } } },
              { "name": "salah-rc", "node": "BILLER",
                "request": { "msgtype": "0200", "tran_type": "INQ", "trace_number": "{{stan}}", "datetime_tran": "{{now}}", "terminal_id": "T1" },
                "expect": { "resp_code": "05" } },
              { "name": "not-supported", "node": "BILLER",
                "request": { "msgtype": "0200", "tran_type": "TRF", "trace_number": "{{stan}}", "datetime_tran": "{{now}}", "terminal_id": "T1" },
                "expect": { "resp_code": "A1", "fields": { "authorized_by": "0" } } },
              { "name": "version", "command": "VERSION", "expect": { "reply": "v1.2.3" } }
            ]
            """);

        await using CoreSim core = await CoreSim.StartAsync(Samples.Options());
        await using var app = await SimInterfaceHost.StartAsync<BillerInterface>(core, configure: o => o.Version = "v1.2.3");

        IReadOnlyList<ScenarioResult> results = await new ScenarioRunner(core).RunAsync(ScenarioRunner.Load(dir));

        Assert.Equal([true, false, true, true], results.Select(r => r.Passed));
        Assert.Contains("resp_code '00', diharapkan '05'.", results[1].Failures);

        string report = Path.Combine(dir, "junit.xml");
        ScenarioRunner.WriteJUnit(report, results);
        XElement suite = XDocument.Load(report).Root!.Element("testsuite")!;
        Assert.Equal("4", (string?)suite.Attribute("tests"));
        Assert.Equal("1", (string?)suite.Attribute("failures"));
    }

    [Fact]
    public void Invalid_scenario_file_is_reported()
    {
        string dir = WriteScenarios("""{ "name": "kosong" }""");
        Assert.Throws<InvalidDataException>(() => ScenarioRunner.Load(dir));
    }
}
