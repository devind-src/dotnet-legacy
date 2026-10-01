using System.Buffers;
using System.Globalization;
using SyncNetPro.Toolkit;

namespace SyncNetPro.Iso8583;

/// <summary>Posisi data sebuah field di wire (tanpa indikator panjang) — dipakai untuk masking hex dump.</summary>
internal readonly record struct IsoFieldRange(int Field, int Offset, int Length);

/// <summary>Pack/parse berdasarkan <see cref="IsoSpec"/>.</summary>
internal static class IsoCodec
{
    public static byte[] Pack(IsoMessage message, List<IsoFieldRange>? ranges = null)
    {
        IsoSpec spec = message.Spec;
        var buffer = new ArrayBufferWriter<byte>(256);

        if (!string.IsNullOrEmpty(message.Tpdu)) Write(buffer, Convert.FromHexString(message.Tpdu));
        else if (spec.TpduLength > 0) throw new IsoFormatException($"TPDU wajib diisi ({spec.TpduLength} byte).");

        Validation.RequireMti(message.Mti);
        Write(buffer, spec.MtiEncoding == IsoEncoding.Bcd ? Bcd.Encode(message.Mti) : Latin1(message.Mti));

        bool secondary = message.Values.Keys.Any(f => f > 64);
        byte[] bitmap = new byte[secondary ? 16 : 8];
        if (secondary) bitmap[0] |= 0x80;
        foreach (int field in message.Values.Keys) bitmap[(field - 1) / 8] |= (byte)(0x80 >> ((field - 1) % 8));
        Write(buffer, spec.BitmapEncoding == IsoEncoding.Bcd ? bitmap : Latin1(Convert.ToHexString(bitmap)));

        foreach ((int number, string value) in message.Values.OrderBy(kv => kv.Key))
        {
            IsoFieldSpec field = Validation.RequireField(spec, number);
            Validation.CheckValue(field, value);
            Validation.CheckBcdByteUnit(spec, field, value);
            byte[] data = EncodeData(field, value);

            if (field.LengthType != IsoLengthType.Fixed)
            {
                int length = field.Encoding == IsoFieldEncoding.Bcd && ByteUnit(spec, field) ? data.Length : value.Length;
                Write(buffer, EncodeLength(spec, field, length));
            }

            ranges?.Add(new IsoFieldRange(number, buffer.WrittenCount, data.Length));
            Write(buffer, data);
        }

        return buffer.WrittenSpan.ToArray();
    }

    public static IsoMessage Parse(IsoSpec spec, ReadOnlySpan<byte> data, List<IsoFieldRange>? ranges = null)
    {
        var reader = new Reader(data);
        var message = new IsoMessage(spec);

        if (spec.TpduLength > 0) message.Tpdu = Convert.ToHexString(reader.Take(spec.TpduLength, null, "TPDU"));

        message.Mti = spec.MtiEncoding == IsoEncoding.Bcd
            ? Bcd.Decode(reader.Take(2, null, "MTI"))
            : Text(reader.Take(4, null, "MTI"));

        byte[] bitmap = ReadBitmap(spec, ref reader);
        int maxField = bitmap.Length * 8;
        for (int number = 2; number <= maxField; number++)
        {
            if ((bitmap[(number - 1) / 8] & (0x80 >> ((number - 1) % 8))) == 0) continue;

            int start = reader.Position;
            IsoFieldSpec field = spec.GetField(number)
                ?? throw new IsoFormatException($"Field {number} ada di bitmap tetapi tidak didefinisikan di spesifikasi.", number, start);

            string value = ReadField(spec, field, ref reader, ranges);
            try
            {
                Validation.CheckValue(field, value);
            }
            catch (IsoFormatException ex)
            {
                throw new IsoFormatException(ex.Message, number, start);
            }

            message.Values[number] = value;
        }

        return message;
    }

    private static byte[] ReadBitmap(IsoSpec spec, ref Reader reader)
    {
        if (spec.BitmapEncoding == IsoEncoding.Bcd)
        {
            byte[] primary = reader.Take(8, null, "bitmap").ToArray();
            return (primary[0] & 0x80) == 0 ? primary : [.. primary, .. reader.Take(8, null, "bitmap sekunder")];
        }

        byte[] first = HexBitmap(reader.Take(16, null, "bitmap"), reader.Position);
        return (first[0] & 0x80) == 0 ? first : [.. first, .. HexBitmap(reader.Take(16, null, "bitmap sekunder"), reader.Position)];
    }

    private static byte[] HexBitmap(ReadOnlySpan<byte> text, int offset)
    {
        try
        {
            return Convert.FromHexString(Text(text));
        }
        catch (FormatException)
        {
            throw new IsoFormatException($"Bitmap bukan hex: '{HexDump.Printable(Text(text))}'.", null, offset);
        }
    }

    private static string ReadField(IsoSpec spec, IsoFieldSpec field, ref Reader reader, List<IsoFieldRange>? ranges)
    {
        int number = field.Number;
        int length = field.LengthType == IsoLengthType.Fixed ? field.Length : ReadLength(spec, field, ref reader);
        bool bcd = field.Encoding == IsoFieldEncoding.Bcd;
        bool byteUnit = bcd && field.LengthType != IsoLengthType.Fixed && ByteUnit(spec, field);
        int byteCount = !bcd ? length : byteUnit ? length : Bcd.ByteCount(length);

        int offset = reader.Position;
        ReadOnlySpan<byte> data = reader.Take(byteCount, number, $"data field {number}");
        ranges?.Add(new IsoFieldRange(number, offset, byteCount));

        return field.Encoding switch
        {
            IsoFieldEncoding.Bcd when byteUnit => Bcd.Decode(data),
            IsoFieldEncoding.Bcd => Bcd.Decode(data, length, field.PadRight),
            IsoFieldEncoding.Ebcdic => Ebcdic.Decode(data),
            _ => Text(data),
        };
    }

    private static int ReadLength(IsoSpec spec, IsoFieldSpec field, ref Reader reader)
    {
        int offset = reader.Position;
        string digits = spec.LengthEncoding == IsoEncoding.Bcd
            ? Bcd.Decode(reader.Take(Bcd.ByteCount(field.LengthDigits), field.Number, $"panjang field {field.Number}"))
            : Text(reader.Take(field.LengthDigits, field.Number, $"panjang field {field.Number}"));

        if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out int length))
        {
            throw new IsoFormatException($"Indikator panjang field {field.Number} bukan angka: '{HexDump.Printable(digits)}'.", field.Number, offset);
        }

        return length;
    }

    private static byte[] EncodeData(IsoFieldSpec field, string value) => field.Encoding switch
    {
        IsoFieldEncoding.Bcd => Bcd.Encode(value, field.PadRight),
        IsoFieldEncoding.Ebcdic => Ebcdic.Encode(value),
        _ => Latin1(value),
    };

    private static byte[] EncodeLength(IsoSpec spec, IsoFieldSpec field, int length)
    {
        string digits = length.ToString(CultureInfo.InvariantCulture).PadLeft(field.LengthDigits, '0');
        if (digits.Length > field.LengthDigits)
        {
            throw new IsoFormatException($"Panjang field {field.Number} ({length}) tidak muat di indikator {field.LengthDigits} digit.", field.Number);
        }

        return spec.LengthEncoding == IsoEncoding.Bcd ? Bcd.Encode(digits) : Latin1(digits);
    }

    /// <summary>Default SDK lama: field BCD 55, atau indikator panjang ASCII, dihitung dalam byte.</summary>
    internal static bool ByteUnit(IsoSpec spec, IsoFieldSpec field) =>
        (field.LengthUnit ?? (field.Number == 55 || spec.LengthEncoding == IsoEncoding.Ascii ? IsoLengthUnit.Bytes : IsoLengthUnit.Characters)) == IsoLengthUnit.Bytes;

    private static byte[] Latin1(string text) => System.Text.Encoding.Latin1.GetBytes(text);

    private static string Text(ReadOnlySpan<byte> bytes) => System.Text.Encoding.Latin1.GetString(bytes);

    private static void Write(ArrayBufferWriter<byte> buffer, ReadOnlySpan<byte> bytes) => buffer.Write(bytes);

    private ref struct Reader(ReadOnlySpan<byte> data)
    {
        private readonly ReadOnlySpan<byte> _data = data;

        public int Position { get; private set; }

        public ReadOnlySpan<byte> Take(int count, int? field, string what)
        {
            if (count > _data.Length - Position)
            {
                throw new IsoFormatException($"Pesan terpotong saat membaca {what}: butuh {count} byte di posisi {Position}, tersisa {_data.Length - Position}.", field, Position);
            }

            ReadOnlySpan<byte> slice = _data.Slice(Position, count);
            Position += count;
            return slice;
        }
    }
}
