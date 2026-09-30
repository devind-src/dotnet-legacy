namespace SyncNetPro.Iso8583;

internal static class Validation
{
    public static void RequireMti(string? mti)
    {
        if (mti is null || mti.Length != 4 || !mti.All(char.IsAsciiDigit))
        {
            throw new IsoFormatException($"MTI harus 4 digit, bukan '{mti}'.");
        }
    }

    public static IsoFieldSpec RequireField(IsoSpec spec, int number)
    {
        if (number is < 2 or > 128)
        {
            throw new IsoFormatException($"Nomor field {number} di luar 2–128 (field 1 adalah bitmap sekunder dan diisi otomatis).", number);
        }

        return spec.GetField(number)
            ?? throw new IsoFormatException($"Field {number} tidak didefinisikan di spesifikasi; tambahkan dengan IsoSpecBuilder.Field({number}, ...).", number);
    }

    public static void CheckValue(IsoFieldSpec field, string value)
    {
        int number = field.Number;

        if (field.LengthType == IsoLengthType.Fixed ? value.Length != field.Length : value.Length > field.Length)
        {
            string rule = field.LengthType == IsoLengthType.Fixed ? $"tepat {field.Length}" : $"maksimal {field.Length}";
            throw new IsoFormatException($"Field {number} ({field.Name}) harus {rule} karakter, diberi {value.Length}.", number);
        }

        switch (field.Encoding)
        {
            case IsoFieldEncoding.Bcd:
                if (!value.All(char.IsAsciiHexDigit))
                {
                    throw new IsoFormatException($"Field {number} ({field.Name}) BCD hanya boleh berisi 0–9/A–F.", number);
                }

                break;

            case IsoFieldEncoding.Ebcdic:
                if (value.Any(c => c > 'ÿ'))
                {
                    throw new IsoFormatException($"Field {number} ({field.Name}) berisi karakter yang tidak dapat di-encode EBCDIC.", number);
                }

                break;

            default:
                int bad = value.AsSpan().IndexOfAnyExceptInRange('\0', 'ÿ');
                if (bad >= 0)
                {
                    throw new IsoFormatException($"Field {number} ({field.Name}) berisi karakter '{value[bad]}' di luar 1 byte (Latin-1).", number);
                }

                break;
        }

        // '*' diizinkan pada PAN/track 2 yang sudah dimasking (PCI DSS), sama dengan SDK lama.
        if (field.Content == IsoFieldContent.N && field.Encoding != IsoFieldEncoding.Bcd
            && !value.All(c => char.IsAsciiDigit(c) || (c == '*' && number is 2 or 35)))
        {
            throw new IsoFormatException($"Field {number} ({field.Name}) harus numerik.", number);
        }
    }

    public static void CheckBcdByteUnit(IsoSpec spec, IsoFieldSpec field, string value)
    {
        if (field.Encoding == IsoFieldEncoding.Bcd && field.LengthType != IsoLengthType.Fixed
            && IsoCodec.ByteUnit(spec, field) && value.Length % 2 != 0)
        {
            throw new IsoFormatException(
                $"Field {field.Number} ({field.Name}) BCD dengan indikator panjang dalam byte harus berjumlah digit genap; " +
                "untuk panjang ganjil set lengthUnit: IsoLengthUnit.Characters.", field.Number);
        }
    }
}
