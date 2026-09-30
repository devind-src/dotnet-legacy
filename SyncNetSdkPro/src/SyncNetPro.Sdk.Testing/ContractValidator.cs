using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SyncNetPro.Contracts;

namespace SyncNetPro.Sdk.Testing;

/// <summary>
/// Memeriksa pesan dari interface terhadap kontrak Core (dok. 02): properti di luar skema (keputusan Q1),
/// tipe nilai, dan kelengkapan response. Hasilnya peringatan yang mudah dipahami developer.
/// </summary>
public static class ContractValidator
{
    private static readonly Dictionary<string, JTokenType[]> RequestSchema = Schema(typeof(CoreRequest));
    private static readonly Dictionary<string, JTokenType[]> ResponseSchema = Schema(typeof(CoreResponse));

    /// <summary>Validasi request dari interface (kanal source).</summary>
    public static IReadOnlyList<string> ValidateRequest(JObject message) => Validate(message, RequestSchema, isResponse: false);

    /// <summary>Validasi response dari interface (kanal sink).</summary>
    public static IReadOnlyList<string> ValidateResponse(JObject message) => Validate(message, ResponseSchema, isResponse: true);

    private static List<string> Validate(JObject message, Dictionary<string, JTokenType[]> schema, bool isResponse)
    {
        ArgumentNullException.ThrowIfNull(message);
        var warnings = new List<string>();
        Walk(message, string.Empty, schema, warnings);

        if (isResponse)
        {
            if (string.IsNullOrWhiteSpace((string?)message["resp_code"])) warnings.Add("resp_code kosong — Core tidak dapat menentukan hasil transaksi.");
            if (string.IsNullOrWhiteSpace((string?)message["authorized_by"])) warnings.Add("authorized_by kosong — isi \"0\" (internal) atau \"1\" (external).");
        }

        foreach (string key in new[] { "tran_type", "datetime_tran", "trace_number", "terminal_id" })
        {
            if (string.IsNullOrEmpty((string?)message[key])) warnings.Add($"{key} kosong — bagian dari kunci korelasi Core.");
        }

        return warnings;
    }

    private static void Walk(JObject obj, string prefix, Dictionary<string, JTokenType[]> schema, List<string> warnings)
    {
        foreach (JProperty property in obj.Properties())
        {
            string path = prefix + property.Name;
            if (!schema.TryGetValue(path, out JTokenType[]? allowed))
            {
                warnings.Add($"Properti '{path}' tidak ada di kontrak Core — taruh data tambahan di additional_data (keputusan Q1).");
                continue;
            }

            JTokenType actual = property.Value.Type;
            if (actual != JTokenType.Null && !allowed.Contains(actual))
            {
                warnings.Add($"Properti '{path}' bertipe {actual}, seharusnya {string.Join("/", allowed)}.");
            }

            if (property.Value is JObject child && schema.Keys.Any(k => k.StartsWith(path + ".", StringComparison.Ordinal)))
            {
                Walk(child, path + ".", schema, warnings);
            }
        }
    }

    private static Dictionary<string, JTokenType[]> Schema(Type root)
    {
        var schema = new Dictionary<string, JTokenType[]>(StringComparer.Ordinal);
        Add(root, string.Empty, schema);
        return schema;
    }

    private static void Add(Type type, string prefix, Dictionary<string, JTokenType[]> schema)
    {
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            string? name = property.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName;
            if (name is null) continue;

            Type t = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            string path = prefix + name;
            schema[path] = t == typeof(string) ? [JTokenType.String]
                : t == typeof(decimal) ? [JTokenType.Integer, JTokenType.Float]
                : t == typeof(int) || t.IsEnum ? [JTokenType.Integer]
                : t == typeof(bool) ? [JTokenType.Boolean]
                : t == typeof(object) ? [JTokenType.Object, JTokenType.Array, JTokenType.String, JTokenType.Integer, JTokenType.Float, JTokenType.Boolean]
                : [JTokenType.Object];

            if (t.Namespace == typeof(CoreRequest).Namespace && t.IsClass && t != typeof(string)) Add(t, path + ".", schema);
        }
    }
}
