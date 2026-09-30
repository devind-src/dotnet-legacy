using System.Buffers.Binary;

namespace SyncNetPro.Contracts;

/// <summary>
/// Framing TCP kanal interface ↔ Core (dok. 02 §1.1): header 2 byte biner big-endian berisi
/// panjang payload saja (exclude header), payload maksimum 65.535 byte.
/// </summary>
public static class CoreFrame
{
    /// <summary>Panjang header dalam byte.</summary>
    public const int HeaderLength = 2;

    /// <summary>Panjang payload maksimum.</summary>
    public const int MaxPayloadLength = ushort.MaxValue;

    /// <summary>Membungkus payload dengan header panjang.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Payload kosong atau lebih dari <see cref="MaxPayloadLength"/>.</exception>
    public static byte[] Encode(ReadOnlySpan<byte> payload)
    {
        if (payload.IsEmpty || payload.Length > MaxPayloadLength)
        {
            throw new ArgumentOutOfRangeException(nameof(payload), payload.Length,
                $"Payload ke Core harus 1–{MaxPayloadLength} byte.");
        }

        var frame = new byte[HeaderLength + payload.Length];
        BinaryPrimitives.WriteUInt16BigEndian(frame, (ushort)payload.Length);
        payload.CopyTo(frame.AsSpan(HeaderLength));
        return frame;
    }

    /// <summary>Membaca panjang payload dari header.</summary>
    public static int ReadPayloadLength(ReadOnlySpan<byte> header)
    {
        if (header.Length < HeaderLength)
            throw new ArgumentException($"Header Core minimal {HeaderLength} byte.", nameof(header));

        return BinaryPrimitives.ReadUInt16BigEndian(header);
    }

    /// <summary>
    /// Mencoba mengambil satu frame lengkap dari awal <paramref name="buffer"/>.
    /// </summary>
    /// <param name="buffer">Data yang sudah diterima.</param>
    /// <param name="payload">Payload frame bila lengkap.</param>
    /// <param name="consumed">Jumlah byte yang dipakai (header + payload).</param>
    /// <returns><c>true</c> bila satu frame lengkap tersedia.</returns>
    /// <exception cref="InvalidDataException">Header berisi panjang 0 (frame rusak).</exception>
    public static bool TryDecode(ReadOnlySpan<byte> buffer, out ReadOnlySpan<byte> payload, out int consumed)
    {
        payload = default;
        consumed = 0;

        if (buffer.Length < HeaderLength) return false;

        int length = ReadPayloadLength(buffer);
        if (length == 0) throw new InvalidDataException("Frame dari Core dengan panjang payload 0.");
        if (buffer.Length < HeaderLength + length) return false;

        payload = buffer.Slice(HeaderLength, length);
        consumed = HeaderLength + length;
        return true;
    }
}
