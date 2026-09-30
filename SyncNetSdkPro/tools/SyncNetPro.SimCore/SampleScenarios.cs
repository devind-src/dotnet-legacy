using Newtonsoft.Json;
using SyncNetPro.Sdk.Testing;

namespace SyncNetPro.SimCore;

internal static class SampleScenarios
{
    public static string Inquiry(string node) => $$"""
        [
          {
            "name": "inquiry-sukses",
            "node": "{{node}}",
            "request": {
              "msgtype": "0200",
              "tran_type": "INQ",
              "tran_type_ext": "PLNPOST",
              "amount_tran": 0,
              "trace_number": "{{"{{stan}}"}}",
              "datetime_tran": "{{"{{now:MMddHHmmss}}"}}",
              "terminal_id": "TERM0001",
              "merchant_id": "MERCHANT01",
              "refnum": "{{"{{rrn}}"}}",
              "to_acc_number": "532110000001"
            },
            "expect": { "resp_code": "00", "within_ms": 5000 }
          }
        ]
        """;

    public const string Version = """
        { "name": "version", "command": "VERSION" }
        """;

    public static string InterfaceSettings(SimCoreOptions options) => JsonConvert.SerializeObject(new
    {
        SyncNet = new
        {
            Home = ".syncnet",
            NodeSource = "Json",
            Command = new { Port = options.Interface?.Port },
            Trace = new { LogServicesHost = "127.0.0.1", LogServicesPort = options.LogServicesPort },
            Nodes = options.Nodes.Select(n => new { n.Name, Category = n.Category.ToString(), n.PortIn, n.PortOut, n.RequestTimeoutSeconds }),
        },
    }, Formatting.Indented);
}
