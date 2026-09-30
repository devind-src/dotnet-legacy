using Newtonsoft.Json.Linq;

namespace SyncNetPro.Contracts.Tests;

/// <summary>Keputusan Q1: skema JSON ke Core tidak boleh bertambah atau berubah.</summary>
public class SchemaTests
{
    private static readonly NewtonsoftCoreMessageSerializer Serializer = NewtonsoftCoreMessageSerializer.Instance;

    [Theory]
    [InlineData("request-empty.json", typeof(CoreRequest))]
    [InlineData("response-empty.json", typeof(CoreResponse))]
    public void Property_names_and_order_equal_legacy_schema(string goldenFile, Type type)
    {
        JObject legacy = JObject.Parse(GoldenFiles.ReadText(goldenFile));
        JObject current = JObject.Parse(Serializer.Serialize(Activator.CreateInstance(type)));

        Assert.Equal(Paths(legacy), Paths(current));
    }

    [Fact]
    public void Extra_information_goes_to_additional_data_not_new_properties()
    {
        var request = new CoreRequest().SetAdditionalData("customer_name", "BUDI");

        JObject json = JObject.Parse(Serializer.Serialize(request));

        Assert.Equal("BUDI", json["additional_data"]!["customer_name"]!.Value<string>());
        Assert.Equal(Paths(JObject.Parse(GoldenFiles.ReadText("request-empty.json"))), Paths(json, skipAdditionalData: true));
    }

    [Fact]
    public void Unknown_properties_from_core_are_ignored()
    {
        // private_data dari Core membawa properti yang tidak dimiliki interface
        const string fromCore = """
            {"tran_type":"PAY","trace_number":"000001","unknown_top":"x",
             "private_data":{"source_node":"CHANNEL","sink_node":"BILLER","mode_timeout":"1","is_req_internal":true,"retry_send":2},
             "security":{"hsm_cmd":1,"hsm_data":"abc","protect_sensitive_data":"1"}}
            """;

        CoreRequest request = Serializer.Deserialize<CoreRequest>(fromCore);
        string json = Serializer.Serialize(request);

        Assert.Equal("BILLER", request.PrivateData!.SinkNode);
        Assert.Equal(2, request.PrivateData.RetrySend);
        Assert.Equal(HsmCommand.GenerateKeyTerminal, request.Security!.HsmCommand);
        Assert.DoesNotContain("unknown_top", json, StringComparison.Ordinal);
        Assert.DoesNotContain("source_node", json, StringComparison.Ordinal);
        Assert.DoesNotContain("hsm_data", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Serializer_is_not_affected_by_global_default_settings()
    {
        var previous = Newtonsoft.Json.JsonConvert.DefaultSettings;
        try
        {
            Newtonsoft.Json.JsonConvert.DefaultSettings = () => new Newtonsoft.Json.JsonSerializerSettings
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                Formatting = Newtonsoft.Json.Formatting.Indented,
            };

            Assert.Equal(GoldenFiles.ReadText("request-empty.json"), Serializer.Serialize(new CoreRequest()));
        }
        finally
        {
            Newtonsoft.Json.JsonConvert.DefaultSettings = previous;
        }
    }

    private static List<string> Paths(JObject obj, bool skipAdditionalData = false, string prefix = "")
    {
        var paths = new List<string>();
        foreach (JProperty property in obj.Properties())
        {
            string path = prefix + property.Name;
            if (skipAdditionalData && path == "additional_data") { paths.Add(path); continue; }

            paths.Add(path);
            if (property.Value is JObject child && path != "additional_data")
            {
                paths.AddRange(Paths(child, skipAdditionalData, path + "."));
            }
        }

        return paths;
    }
}
