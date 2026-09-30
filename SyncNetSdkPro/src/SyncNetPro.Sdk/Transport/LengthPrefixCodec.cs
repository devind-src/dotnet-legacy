using System.Buffers;
using System.Buffers.Binary;
using System.Globalization;

namespace SyncNetPro.Sdk.Transport;

/// <summary>
/// Codec header panjang — kompatibel byte-per-byte dengan <c>TcpHeader</c> SDK lama
/// (diverifikasi golden test <c>tcpheader.json</c>).
/// </summary>
public sealed class LengthPrefixCodec : ITcpFrameCodec
{
    private readonly TcpHeaderOptions _options;

    /// <summary>Membuat codec.</summary>
    public LengthPrefixCodec(TcpHeaderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.MaxPayloadLength <= 0) throw new ArgumentOutOfRangeException(nameof(options), "MaxPayloadLength harus > 0.");
        _options = options;
    }

    /// <summary>Codec default kanal Core (2 byte biner big-endian, exclude).</summary>
    public static LengthPrefixCodec Default { get; } = new(TcpHeaderOptions.Default);

    /// <summary>Opsi header.</summary>
    public TcpHeaderOptions Options => _options;

    /// <summary>Panjang header dalam byte.</summary>
    public int HeaderLength => GetHeaderLength(_options.HeaderType);

    /// <summary>Panjang header untuk jenis tertentu.</summary>
    public static int GetHeaderLength(TcpHeaderType type) => type switch
    {
        TcpHeaderType.Binary2Byte or TcpHeaderType.Bcd2Byte => 2,
        TcpHeaderType.Binary4Byte or TcpHeaderType.Ascii4Digit => 4,
        _ => throw new NotSupportedException($"Header {type} tidak didukung."),
    };

    /// <inheritdoc />
    public byte[] Encode(ReadOnlySpan<byte> payload)
    {
        int headerLength = HeaderLength;
        long encoded = payload.Length + (_options.LengthMode == TcpLengthMode.Include ? headerLength : 0);
        long max = _options.HeaderType switch
        {
            TcpHeaderType.Binary2Byte => ushort.MaxValue,
            TcpHeaderType.Bcd2Byte or TcpHeaderType.Ascii4Digit => 9_999,
            _ => int.MaxValue,
        };

        if (payload.IsEmpty || encoded > max)
        {
            throw new ArgumentOutOfRangeException(nameof(payload), payload.Length,
                $"Payload {payload.Length} byte tidak dapat dikodekan oleh header {_options.HeaderType} ({_options.LengthMode}); batas {max}.");
        }

        var frame = new byte[headerLength + payload.Length];
        Span<byte> header = frame.AsSpan(0, headerLength);
        int length = (int)encoded;

        switch (_options.HeaderType)
        {
            case TcpHeaderType.Binary2Byte:
                if (_options.Endian == TcpEndianMode.BigEndian) BinaryPrimitives.WriteUInt16BigEndian(header, (ushort)length);
                else BinaryPrimitives.WriteUInt16LittleEndian(header, (ushort)length);
                break;
            case TcpHeaderType.Binary4Byte:
                if (_options.Endian == TcpEndianMode.BigEndian) BinaryPrimitives.WriteInt32BigEndian(header, length);
                else BinaryPrimitives.WriteInt32LittleEndian(header, length);
                break;
            case TcpHeaderType.Bcd2Byte:
                string digits = length.ToString("D4", CultureInfo.InvariantCulture);
                header[0] = (byte)(((digits[0] - '0') << 4) | (digits[1] - '0'));
                header[1] = (byte)(((digits[2] - '0') << 4) | (digits[3] - '0'));
                break;
            case TcpHeaderType.Ascii4Digit:
                string ascii = length.ToString("D4", CultureInfo.InvariantCulture);
                for (int i = 0; i < 4; i++) header[i] = (byte)ascii[i];
                break;
        }

        payload.CopyTo(frame.AsSpan(headerLength));
        return frame;
    }

    /// <inheritdoc />
    public bool TryDecode(ref ReadOnlySequence<byte> buffer, out byte[] payload)
    {
        payload = [];
        int headerLength = HeaderLength;
        if (buffer.Length < headerLength) return false;

        Span<byte> header = stackalloc byte[4];
        buffer.Slice(0, headerLength).CopyTo(header);

        int length = ReadPayloadLength(header[..headerLength]);
        if (length <= 0 || length > _options.MaxPayloadLength)
        {
            throw new InvalidDataException($"Panjang pesan tidak valid: {length} (maks. {_options.MaxPayloadLength}).");
        }

        if (buffer.Length < headerLength + length) return false;

        payload = buffer.Slice(headerLength, length).ToArray();
        buffer = buffer.Slice(headerLength + length);
        return true;
    }

    /// <summary>Membaca panjang payload dari header (setara <c>TcpHeader.GetLengthMessage</c>).</summary>
    public int ReadPayloadLength(ReadOnlySpan<byte> header)
    {
        int value = _options.HeaderType switch
        {
            TcpHeaderType.Binary2Byte => _options.Endian == TcpEndianMode.BigEndian
                ? BinaryPrimitives.ReadUInt16BigEndian(header)
                : BinaryPrimitives.ReadUInt16LittleEndian(header),
            TcpHeaderType.Binary4Byte => _options.Endian == TcpEndianMode.BigEndian
                ? BinaryPrimitives.ReadInt32BigEndian(header)
                : BinaryPrimitives.ReadInt32LittleEndian(header),
            TcpHeaderType.Bcd2Byte => ReadBcd(header),
            TcpHeaderType.Ascii4Digit => ReadAscii(header),
            _ => throw new NotSupportedException(),
        };

        return _options.LengthMode == TcpLengthMode.Include ? value - HeaderLength : value;
    }

    private static int ReadBcd(ReadOnlySpan<byte> header)
    {
        int value = 0;
        for (int i = 0; i < 2; i++)
        {
            int hi = header[i] >> 4, lo = header[i] & 0x0F;
            if (hi > 9 || lo > 9) throw new InvalidDataException($"Header BCD tidak valid: {Convert.ToHexString(header)}.");
            value = (value * 100) + (hi * 10) + lo;
        }

        return value;
    }

    private static int ReadAscii(ReadOnlySpan<byte> header)
    {
        int value = 0;
        foreach (byte b in header[..4])
        {
            if (b is < (byte)'0' or > (byte)'9') throw new InvalidDataException($"Header ASCII tidak valid: {Convert.ToHexString(header)}.");
            value = (value * 10) + (b - '0');
        }

        return value;
    }
}
