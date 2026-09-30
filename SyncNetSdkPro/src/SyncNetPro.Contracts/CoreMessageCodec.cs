namespace SyncNetPro.Contracts;

/// <summary>
/// Gabungan serializer + framing: objek pesan ↔ frame TCP siap kirim ke / terima dari Core.
/// </summary>
public sealed class CoreMessageCodec(ICoreMessageSerializer serializer)
{
    /// <summary>Codec default (Newtonsoft, keputusan Q3).</summary>
    public static CoreMessageCodec Default { get; } = new(NewtonsoftCoreMessageSerializer.Instance);

    /// <summary>Serializer yang dipakai.</summary>
    public ICoreMessageSerializer Serializer { get; } = serializer ?? throw new ArgumentNullException(nameof(serializer));

    /// <summary>Serialisasi pesan ke frame (header 2 byte + JSON UTF-8).</summary>
    public byte[] EncodeFrame<T>(T message) => CoreFrame.Encode(Serializer.SerializeToUtf8Bytes(message));

    /// <summary>Deserialisasi payload (tanpa header).</summary>
    public T DecodePayload<T>(ReadOnlySpan<byte> payload) => Serializer.Deserialize<T>(payload);
}
