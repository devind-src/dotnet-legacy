using System.Globalization;
using System.Text;
using SyncNetPro.Contracts;
using SyncNetPro.Toolkit;

namespace SyncNetPro.Iso8583;

/// <summary>
/// Pesan ISO 8583. Nilai field selalu divalidasi terhadap <see cref="Spec"/> saat di-set,
/// sehingga kesalahan muncul di baris yang mengisi field, bukan di sisi penerima.
/// </summary>
/// <example>
/// <code>
/// var iso = new IsoMessage(BillerSpec.Instance, "0200")
///     .Set(3, "380000")
///     .Set(11, stan)
///     .Set(41, terminalId.PadRight(8));
/// byte[] payload = iso.Pack();
/// IsoMessage reply = IsoMessage.Parse(BillerSpec.Instance, received);
/// string rc = reply[39];
/// </code>
/// </example>
public sealed class IsoMessage
{
    private string _mti = string.Empty;
    private string? _tpdu;

    /// <summary>Membuat pesan kosong.</summary>
    /// <param name="spec">Spesifikasi field.</param>
    /// <param name="mti">MTI 4 digit (boleh diisi belakangan).</param>
    public IsoMessage(IsoSpec spec, string? mti = null)
    {
        ArgumentNullException.ThrowIfNull(spec);
        Spec = spec;
        if (!string.IsNullOrEmpty(mti)) Mti = mti;
    }

    /// <summary>Spesifikasi field.</summary>
    public IsoSpec Spec { get; }

    /// <summary>Message Type Indicator, mis. <c>0200</c>.</summary>
    /// <exception cref="IsoFormatException">Bukan 4 digit.</exception>
    public string Mti
    {
        get => _mti;
        set
        {
            Validation.RequireMti(value);
            _mti = value;
        }
    }

    /// <summary>TPDU dalam hex (mis. <c>6000010000</c>), <c>null</c> bila tidak dipakai.</summary>
    /// <exception cref="IsoFormatException">Bukan hex atau panjang tidak sesuai <see cref="IsoSpec.TpduLength"/>.</exception>
    public string? Tpdu
    {
        get => _tpdu;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _tpdu = null;
                return;
            }

            if (!value.All(char.IsAsciiHexDigit) || value.Length % 2 != 0 || (Spec.TpduLength > 0 && value.Length != Spec.TpduLength * 2))
            {
                throw new IsoFormatException($"TPDU harus {Spec.TpduLength} byte hex, bukan '{value}'.");
            }

            _tpdu = value.ToUpperInvariant();
        }
    }

    /// <summary>Nilai field; <c>null</c> bila tidak ada. Mengisi <c>null</c>/kosong menghapus field.</summary>
    /// <exception cref="IsoFormatException">Field tidak didefinisikan atau nilai tidak sesuai spesifikasi.</exception>
    public string? this[int field]
    {
        get => Values.GetValueOrDefault(field);
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                Values.Remove(field);
                return;
            }

            IsoFieldSpec spec = Validation.RequireField(Spec, field);
            if (spec.Encoding == IsoFieldEncoding.Bcd) value = value.ToUpperInvariant();
            Validation.CheckValue(spec, value);
            Validation.CheckBcdByteUnit(Spec, spec, value);
            Values[field] = value;
        }
    }

    /// <summary>Field yang terisi, urut nomor.</summary>
    public IEnumerable<KeyValuePair<int, string>> Fields => Values.OrderBy(kv => kv.Key);

    /// <summary>Request (digit ketiga MTI 0 atau 2).</summary>
    public bool IsRequest => _mti.Length == 4 && _mti[2] is '0' or '2';

    /// <summary>Response (digit ketiga MTI 1 atau 3).</summary>
    public bool IsResponse => _mti.Length == 4 && _mti[2] is '1' or '3';

    internal Dictionary<int, string> Values { get; } = [];

    /// <summary>Parse pesan (tanpa header TCP — framing dilakukan codec transport).</summary>
    /// <exception cref="IsoFormatException">Pesan tidak sesuai spesifikasi; <see cref="IsoFormatException.Field"/> menunjukkan field.</exception>
    public static IsoMessage Parse(IsoSpec spec, ReadOnlySpan<byte> data)
    {
        ArgumentNullException.ThrowIfNull(spec);
        return IsoCodec.Parse(spec, data);
    }

    /// <summary>Seperti <see cref="Parse"/> tanpa exception.</summary>
    public static bool TryParse(IsoSpec spec, ReadOnlySpan<byte> data, out IsoMessage? message, out string? error)
    {
        try
        {
            message = Parse(spec, data);
            error = null;
            return true;
        }
        catch (IsoFormatException ex)
        {
            message = null;
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Trace pesan dari wire: daftar field + hex dump dengan data sensitif disamarkan. Bila parse gagal,
    /// berisi pesan error dan (saat masking aktif) hanya byte sebelum posisi error.
    /// </summary>
    public static string FormatTrace(IsoSpec spec, ReadOnlySpan<byte> data, IsoFormatOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(spec);
        options ??= IsoFormatOptions.Default;
        var ranges = new List<IsoFieldRange>();
        try
        {
            IsoMessage message = IsoCodec.Parse(spec, data, ranges);
            return message.Format(options) + "\n" + HexDump.Format(MaskRanges(data, ranges, options));
        }
        catch (IsoFormatException ex)
        {
            ReadOnlySpan<byte> shown = options.SensitiveFields.Count == 0 && options.PanFields.Count == 0
                ? data
                : data[..Math.Min(ex.Offset ?? 0, data.Length)];
            string note = shown.Length < data.Length ? $"\n({data.Length - shown.Length} byte berikutnya tidak ditampilkan)\n" : string.Empty;
            return ex.Message + "\n" + HexDump.Format(MaskRanges(shown, ranges, options)) + note;
        }
    }

    /// <summary>Mengisi field (fluent).</summary>
    public IsoMessage Set(int field, string? value)
    {
        this[field] = value;
        return this;
    }

    /// <summary>Field terisi.</summary>
    public bool Has(int field) => Values.ContainsKey(field);

    /// <summary>Menghapus field.</summary>
    public IsoMessage Remove(int field)
    {
        Values.Remove(field);
        return this;
    }

    /// <summary>Pack ke byte (TPDU + MTI + bitmap + field) tanpa header TCP.</summary>
    /// <exception cref="IsoFormatException">MTI belum diisi atau pesan tidak valid.</exception>
    public byte[] Pack() => IsoCodec.Pack(this);

    /// <summary>Salinan pesan.</summary>
    public IsoMessage Clone()
    {
        var copy = new IsoMessage(Spec) { _mti = _mti, _tpdu = _tpdu };
        foreach ((int field, string value) in Values) copy.Values[field] = value;
        return copy;
    }

    /// <summary>
    /// Salinan sebagai response: MTI response (<c>0200</c>→<c>0210</c>, <c>0220</c>→<c>0230</c>, dst.) dan
    /// field 39 bila <paramref name="responseCode"/> diisi.
    /// </summary>
    public IsoMessage CreateResponse(string? responseCode = null)
    {
        IsoMessage response = Clone();
        response.Mti = MessageTypes.ToResponse(_mti);
        if (responseCode is not null) response[39] = responseCode;
        return response;
    }

    /// <summary>Daftar field untuk trace (tata letak <c>GetFormattedMessage</c> SDK lama).</summary>
    public string Format(IsoFormatOptions? options = null)
    {
        options ??= IsoFormatOptions.Default;
        var sb = new StringBuilder().Append(_mti).Append('\n');
        (string primary, string? secondary) = BitmapHex();
        AppendLine(sb, 0, "Fixed", "an", 16, primary);
        if (secondary is not null) AppendLine(sb, 1, "Fixed", "an", 16, secondary);

        foreach ((int number, string value) in Fields)
        {
            IsoFieldSpec field = Spec.GetField(number)!;
            AppendLine(sb, number, LengthTypeName(field.LengthType), ContentName(field.Content), field.Length, options.Mask(number, value));
        }

        return sb.Append('\n').ToString();
    }

    /// <summary>Daftar field ringkas (tata letak <c>GetFormattedSimple</c> SDK lama).</summary>
    public string FormatSimple(IsoFormatOptions? options = null)
    {
        options ??= IsoFormatOptions.Default;
        var sb = new StringBuilder("MTI: ").Append(_mti).Append('\n');
        (string primary, string? secondary) = BitmapHex();
        sb.Append("000  [").Append(primary).Append("]\n");
        if (secondary is not null) sb.Append("001  [").Append(secondary).Append("]\n");
        foreach ((int number, string value) in Fields)
        {
            sb.Append(number.ToString("D3", CultureInfo.InvariantCulture)).Append("  [").Append(HexDump.Printable(options.Mask(number, value))).Append("]\n");
        }

        return sb.Append('\n').ToString();
    }

    /// <summary>Trace lengkap: <see cref="Format"/> + hex dump hasil pack (data sensitif disamarkan).</summary>
    public string FormatTrace(IsoFormatOptions? options = null)
    {
        options ??= IsoFormatOptions.Default;
        var ranges = new List<IsoFieldRange>();
        byte[] bytes = IsoCodec.Pack(this, ranges);
        return Format(options) + "\n" + HexDump.Format(MaskRanges(bytes, ranges, options));
    }

    /// <inheritdoc />
    public override string ToString() => FormatSimple();

    private static byte[] MaskRanges(ReadOnlySpan<byte> data, List<IsoFieldRange> ranges, IsoFormatOptions options)
    {
        byte[] copy = data.ToArray();
        foreach (IsoFieldRange range in ranges)
        {
            if (!options.IsMasked(range.Field) || range.Offset >= copy.Length) continue;
            int length = Math.Min(range.Length, copy.Length - range.Offset);
            int keepHead = 0, keepTail = 0;
            if (!options.SensitiveFields.Contains(range.Field) && length > 10)
            {
                keepHead = 6;
                keepTail = 4;
            }

            copy.AsSpan(range.Offset + keepHead, length - keepHead - keepTail).Fill((byte)'*');
        }

        return copy;
    }

    private (string Primary, string? Secondary) BitmapHex()
    {
        bool secondary = Values.Keys.Any(f => f > 64);
        byte[] bitmap = new byte[secondary ? 16 : 8];
        if (secondary) bitmap[0] |= 0x80;
        foreach (int field in Values.Keys) bitmap[(field - 1) / 8] |= (byte)(0x80 >> ((field - 1) % 8));
        string hex = Convert.ToHexString(bitmap);
        return secondary ? (hex[..16], hex[16..]) : (hex, (string?)null);
    }

    private static void AppendLine(StringBuilder sb, int number, string lengthType, string content, int length, string value) =>
        sb.Append('[')
            .Append(lengthType.PadRight(9))
            .Append(content.PadRight(5))
            .Append(length.ToString(CultureInfo.InvariantCulture).PadLeft(6))
            .Append(' ')
            .Append(value.Length.ToString(CultureInfo.InvariantCulture).PadLeft(3, '0'))
            .Append("] ")
            .Append(number.ToString("D3", CultureInfo.InvariantCulture))
            .Append("  [")
            .Append(HexDump.Printable(value))
            .Append("]\n");

    private static string LengthTypeName(IsoLengthType type) => type == IsoLengthType.Fixed ? "Fixed" : new string('L', (int)type) + "VAR";

    private static string ContentName(IsoFieldContent content) => content switch
    {
        IsoFieldContent.N => "n",
        IsoFieldContent.An => "an",
        _ => "ans",
    };
}
