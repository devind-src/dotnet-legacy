namespace SyncNetPro.Contracts;

/// <summary>
/// Serializer payload JSON antara interface dan Core. Implementasi wajib lulus golden test
/// kompatibilitas terhadap SDK lama (dok. 02 §2, dok. 08 §2.1).
/// </summary>
public interface ICoreMessageSerializer
{
    /// <summary>Serialisasi ke teks JSON (satu baris, tanpa indentasi).</summary>
    string Serialize<T>(T message);

    /// <summary>Serialisasi ke byte UTF-8 (tanpa BOM).</summary>
    byte[] SerializeToUtf8Bytes<T>(T message);

    /// <summary>Deserialisasi teks JSON.</summary>
    T Deserialize<T>(string json);

    /// <summary>Deserialisasi byte UTF-8.</summary>
    T Deserialize<T>(ReadOnlySpan<byte> utf8Json);
}
