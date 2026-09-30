using System.Text;
using Newtonsoft.Json;

namespace SyncNetPro.Contracts;

/// <summary>
/// Serializer default (versi 1.x, keputusan Q3): Newtonsoft.Json dengan setting yang setara
/// <c>JsonConvert.SerializeObject(obj)</c> tanpa <c>DefaultSettings</c> global — sama dengan SDK lama
/// dan Core: null tetap ditulis, decimal bulat bertitik (<c>10000.0</c>), enum sebagai angka,
/// properti tak dikenal diabaikan.
/// </summary>
public sealed class NewtonsoftCoreMessageSerializer : ICoreMessageSerializer
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    // Setting eksplisit agar tidak terpengaruh JsonConvert.DefaultSettings yang mungkin diubah aplikasi.
    private static readonly JsonSerializerSettings Settings = new()
    {
        Formatting = Formatting.None,
        NullValueHandling = NullValueHandling.Include,
        DefaultValueHandling = DefaultValueHandling.Include,
        MissingMemberHandling = MissingMemberHandling.Ignore,
        TypeNameHandling = TypeNameHandling.None,
        DateParseHandling = DateParseHandling.DateTime,
        FloatParseHandling = FloatParseHandling.Double,
        MaxDepth = 64,
    };

    /// <summary>Instance bersama (stateless, thread-safe).</summary>
    public static NewtonsoftCoreMessageSerializer Instance { get; } = new();

    /// <inheritdoc />
    public string Serialize<T>(T message) => JsonConvert.SerializeObject(message, typeof(T), Settings);

    /// <inheritdoc />
    public byte[] SerializeToUtf8Bytes<T>(T message) => Utf8NoBom.GetBytes(Serialize(message));

    /// <inheritdoc />
    public T Deserialize<T>(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return JsonConvert.DeserializeObject<T>(json, Settings)
            ?? throw new JsonSerializationException($"Pesan {typeof(T).Name} kosong (null).");
    }

    /// <inheritdoc />
    public T Deserialize<T>(ReadOnlySpan<byte> utf8Json) => Deserialize<T>(Utf8NoBom.GetString(utf8Json));
}
