using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SyncNetPro.Contracts;

namespace SyncNetPro.Sdk.Testing;

/// <summary>Satu skenario uji (file JSON di <c>simcore/scenarios</c>).</summary>
public sealed class SimScenario
{
    /// <summary>Nama skenario.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Node tujuan (untuk skenario request).</summary>
    public string? Node { get; set; }

    /// <summary>Request Core → interface (kanal sink); nilai string boleh memakai placeholder <c>{{...}}</c>.</summary>
    public JObject? Request { get; set; }

    /// <summary>Command ke command port interface (alternatif dari <see cref="Request"/>).</summary>
    public string? Command { get; set; }

    /// <summary>Batas waktu (ms); default timeout node.</summary>
    public int? TimeoutMs { get; set; }

    /// <summary>Harapan hasil.</summary>
    public SimExpectation? Expect { get; set; }

    /// <summary>File asal.</summary>
    [JsonIgnore]
    public string? Source { get; set; }
}

/// <summary>Harapan hasil skenario.</summary>
public sealed class SimExpectation
{
    /// <summary>Hasil (<c>Responded</c>, <c>Timeout</c>, <c>LinkDown</c>, <c>Duplicate</c>); default <c>Responded</c>.</summary>
    public SimOutcome? Outcome { get; set; }

    /// <summary><c>resp_code</c> yang diharapkan.</summary>
    [JsonProperty("resp_code")]
    public string? ResponseCode { get; set; }

    /// <summary>Batas waktu balasan (ms).</summary>
    [JsonProperty("within_ms")]
    public int? WithinMs { get; set; }

    /// <summary>Nilai field response (path JSON, mis. <c>additional_data.customer_name</c>).</summary>
    public Dictionary<string, JToken?>? Fields { get; set; }

    /// <summary>Balasan command yang diharapkan.</summary>
    public string? Reply { get; set; }

    /// <summary>Gagal bila ada peringatan kontrak (default <c>true</c>).</summary>
    public bool NoContractWarnings { get; set; } = true;
}

/// <summary>Hasil satu skenario.</summary>
/// <param name="Name">Nama.</param>
/// <param name="Passed">Lulus.</param>
/// <param name="Failures">Alasan gagal.</param>
/// <param name="Elapsed">Durasi.</param>
/// <param name="Outcome">Hasil pengiriman (skenario request).</param>
/// <param name="Response">Response (JSON) atau balasan command.</param>
public sealed record ScenarioResult(string Name, bool Passed, IReadOnlyList<string> Failures, TimeSpan Elapsed, SimOutcome? Outcome, string? Response);

/// <summary>Memuat dan menjalankan skenario.</summary>
public sealed partial class ScenarioRunner(SimCore core)
{
    private static readonly JsonSerializerSettings Settings = new() { Converters = { new Newtonsoft.Json.Converters.StringEnumConverter() } };
    private int _stan = RandomNumberGenerator.GetInt32(1, 900_000);

    /// <summary>Memuat skenario dari file atau folder (<c>*.json</c>, satu objek atau array per file).</summary>
    public static IReadOnlyList<SimScenario> Load(string path)
    {
        IEnumerable<string> files = Directory.Exists(path)
            ? Directory.GetFiles(path, "*.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal)
            : [path];

        var scenarios = new List<SimScenario>();
        foreach (string file in files)
        {
            JToken token = JToken.Parse(File.ReadAllText(file));
            IEnumerable<JToken> items = token is JArray array ? array : [token];
            foreach (JToken item in items)
            {
                SimScenario scenario = item.ToObject<SimScenario>(JsonSerializer.Create(Settings))
                    ?? throw new InvalidDataException($"Skenario kosong di {file}.");
                scenario.Source = file;
                if (string.IsNullOrWhiteSpace(scenario.Name)) scenario.Name = Path.GetFileNameWithoutExtension(file);
                if (scenario.Request is null && scenario.Command is null)
                    throw new InvalidDataException($"Skenario {scenario.Name} ({file}) harus berisi 'request' atau 'command'.");
                scenarios.Add(scenario);
            }
        }

        return scenarios;
    }

    /// <summary>Menjalankan skenario berurutan.</summary>
    public async Task<IReadOnlyList<ScenarioResult>> RunAsync(IEnumerable<SimScenario> scenarios, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scenarios);
        var results = new List<ScenarioResult>();
        foreach (SimScenario scenario in scenarios) results.Add(await RunAsync(scenario, cancellationToken).ConfigureAwait(false));
        return results;
    }

    /// <summary>Menjalankan satu skenario.</summary>
    public async Task<ScenarioResult> RunAsync(SimScenario scenario, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        var watch = Stopwatch.StartNew();
        var failures = new List<string>();
        SimExpectation expect = scenario.Expect ?? new SimExpectation();

        try
        {
            if (scenario.Command is not null)
            {
                string reply = await core.CommandAsync(scenario.Command, cancellationToken).ConfigureAwait(false);
                if (expect.Reply is not null && reply != expect.Reply) failures.Add($"Balasan command '{reply}', diharapkan '{expect.Reply}'.");
                return new ScenarioResult(scenario.Name, failures.Count == 0, failures, watch.Elapsed, null, reply);
            }

            string node = scenario.Node ?? core.Nodes.FirstOrDefault()?.Name ?? throw new InvalidOperationException("Skenario tanpa node dan SimCore tanpa node.");
            CoreRequest request = BuildRequest(scenario.Request!);
            TimeSpan? timeout = scenario.TimeoutMs is int ms ? TimeSpan.FromMilliseconds(ms) : null;
            SimResult result = await core.SendAsync(node, request, timeout, cancellationToken).ConfigureAwait(false);
            string? responseJson = result.Response is null ? null : CoreMessageCodec.Default.Serializer.Serialize(result.Response);

            SimOutcome expectedOutcome = expect.Outcome ?? SimOutcome.Responded;
            if (result.Outcome != expectedOutcome) failures.Add($"Hasil {result.Outcome}, diharapkan {expectedOutcome}.");
            if (expect.ResponseCode is not null && result.Response?.ResponseCode != expect.ResponseCode)
                failures.Add($"resp_code '{result.Response?.ResponseCode}', diharapkan '{expect.ResponseCode}'.");
            if (expect.WithinMs is int within && result.Elapsed.TotalMilliseconds > within)
                failures.Add($"Balasan {result.Elapsed.TotalMilliseconds:0} ms, melebihi {within} ms.");
            if (expect.NoContractWarnings) failures.AddRange(result.Warnings.Select(w => "Kontrak: " + w));

            if (expect.Fields is not null)
            {
                JObject actual = responseJson is null ? [] : JObject.Parse(responseJson);
                foreach ((string path, JToken? expected) in expect.Fields)
                {
                    JToken? value = actual.SelectToken(path);
                    if (!JToken.DeepEquals(value ?? JValue.CreateNull(), expected ?? JValue.CreateNull()))
                        failures.Add($"{path} = {value?.ToString(Formatting.None) ?? "null"}, diharapkan {expected?.ToString(Formatting.None) ?? "null"}.");
                }
            }

            return new ScenarioResult(scenario.Name, failures.Count == 0, failures, watch.Elapsed, result.Outcome, responseJson);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            failures.Add($"{ex.GetType().Name}: {ex.Message}");
            return new ScenarioResult(scenario.Name, false, failures, watch.Elapsed, null, null);
        }
    }

    /// <summary>Membentuk request dari template skenario (placeholder diganti).</summary>
    public CoreRequest BuildRequest(JObject template)
    {
        ArgumentNullException.ThrowIfNull(template);
        var copy = (JObject)template.DeepClone();
        string stan = (Interlocked.Increment(ref _stan) % 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        foreach (JValue value in copy.Descendants().OfType<JValue>().Where(v => v.Type == JTokenType.String).ToList())
        {
            value.Value = Expand((string)value.Value!, stan);
        }

        return CoreMessageCodec.Default.Serializer.Deserialize<CoreRequest>(copy.ToString(Formatting.None));
    }

    /// <summary>
    /// Placeholder: <c>{{stan}}</c> (6 digit, sama dalam satu skenario), <c>{{now:format}}</c>, <c>{{rrn}}</c>
    /// (yyMMdd + stan), <c>{{random:n}}</c> (n digit), <c>{{env:NAMA}}</c>.
    /// </summary>
    internal static string Expand(string text, string stan) =>
        Placeholder().Replace(text, m =>
        {
            string name = m.Groups["name"].Value;
            string arg = m.Groups["arg"].Value;
            return name switch
            {
                "stan" => stan,
                "rrn" => DateTime.Now.ToString("yyMMdd", CultureInfo.InvariantCulture) + stan,
                "now" => DateTime.Now.ToString(string.IsNullOrEmpty(arg) ? "MMddHHmmss" : arg, CultureInfo.InvariantCulture),
                "random" => RandomDigits(int.TryParse(arg, out int n) ? n : 6),
                "env" => Environment.GetEnvironmentVariable(arg) ?? string.Empty,
                _ => m.Value,
            };
        });

    private static string RandomDigits(int count)
    {
        var sb = new StringBuilder(count);
        for (int i = 0; i < count; i++) sb.Append((char)('0' + RandomNumberGenerator.GetInt32(10)));
        return sb.ToString();
    }

    [GeneratedRegex(@"\{\{(?<name>[a-z]+)(:(?<arg>[^}]*))?\}\}")]
    private static partial Regex Placeholder();

    /// <summary>Menulis laporan JUnit XML (untuk CI).</summary>
    public static void WriteJUnit(string path, IReadOnlyList<ScenarioResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        var suite = new XElement("testsuite",
            new XAttribute("name", "simcore"),
            new XAttribute("tests", results.Count),
            new XAttribute("failures", results.Count(r => !r.Passed)),
            new XAttribute("time", results.Sum(r => r.Elapsed.TotalSeconds).ToString("0.000", CultureInfo.InvariantCulture)),
            results.Select(r => new XElement("testcase",
                new XAttribute("classname", "simcore"),
                new XAttribute("name", r.Name),
                new XAttribute("time", r.Elapsed.TotalSeconds.ToString("0.000", CultureInfo.InvariantCulture)),
                r.Passed ? null : new XElement("failure", new XAttribute("message", r.Failures.Count > 0 ? r.Failures[0] : "gagal"), string.Join('\n', r.Failures)))));
        new XDocument(new XElement("testsuites", suite)).Save(path);
    }
}
