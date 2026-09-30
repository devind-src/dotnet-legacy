using System.Buffers;

namespace SyncNetPro.Sdk.Transport;

/// <summary>
/// Framing tanpa header (<c>protocol = 5</c>): setiap blok data yang tersedia pada satu kali pembacaan
/// socket dianggap satu pesan, dan pesan dikirim apa adanya. SDK lama tidak benar-benar mendukung mode ini
/// (jatuh ke header 2 byte — bug B2).
/// </summary>
public sealed class NoHeaderCodec : ITcpFrameCodec
{
    /// <summary>Instance bersama.</summary>
    public static NoHeaderCodec Instance { get; } = new();

    /// <inheritdoc />
    public byte[] Encode(ReadOnlySpan<byte> payload)
    {
        if (payload.IsEmpty) throw new ArgumentOutOfRangeException(nameof(payload), "Payload kosong.");
        return payload.ToArray();
    }

    /// <inheritdoc />
    public bool TryDecode(ref ReadOnlySequence<byte> buffer, out byte[] payload)
    {
        payload = [];
        if (buffer.IsEmpty) return false;

        payload = buffer.ToArray();
        buffer = buffer.Slice(buffer.End);
        return true;
    }
}
