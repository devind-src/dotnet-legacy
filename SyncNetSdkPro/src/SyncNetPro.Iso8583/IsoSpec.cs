using System.Collections.Frozen;

namespace SyncNetPro.Iso8583;

/// <summary>
/// Spesifikasi pesan ISO 8583 (immutable): encoding MTI/bitmap/indikator panjang, panjang TPDU,
/// dan definisi field. Buat dengan <see cref="Create"/> atau turunkan dari spesifikasi lain via <see cref="ToBuilder"/>.
/// </summary>
public sealed class IsoSpec
{
    private readonly FrozenDictionary<int, IsoFieldSpec> _fields;

    internal IsoSpec(IsoSpecBuilder builder)
    {
        MtiEncoding = builder.MtiEncoding;
        BitmapEncoding = builder.BitmapEncoding;
        LengthEncoding = builder.LengthEncoding;
        TpduLength = builder.TpduLength;
        _fields = builder.Fields.ToFrozenDictionary();
    }

    /// <summary>Encoding MTI.</summary>
    public IsoEncoding MtiEncoding { get; }

    /// <summary>Encoding bitmap.</summary>
    public IsoEncoding BitmapEncoding { get; }

    /// <summary>Encoding indikator panjang field variabel.</summary>
    public IsoEncoding LengthEncoding { get; }

    /// <summary>Panjang TPDU dalam byte di depan MTI (0 = tanpa TPDU).</summary>
    public int TpduLength { get; }

    /// <summary>Definisi field, urut nomor.</summary>
    public IEnumerable<IsoFieldSpec> Fields => _fields.Values.OrderBy(f => f.Number);

    /// <summary>
    /// Spesifikasi default <c>FieldFormatter</c> SDK lama (semua ASCII). Pakai sebagai dasar lalu
    /// sesuaikan dengan <see cref="ToBuilder"/> — spesifikasi remote biasanya berbeda.
    /// </summary>
    public static IsoSpec Legacy { get; } = LegacySpec.Create();

    /// <summary>Membuat spesifikasi baru.</summary>
    public static IsoSpec Create(Action<IsoSpecBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new IsoSpecBuilder();
        configure(builder);
        return builder.Build();
    }

    /// <summary>Definisi field, atau <c>null</c> bila tidak didefinisikan.</summary>
    public IsoFieldSpec? GetField(int number) => _fields.GetValueOrDefault(number);

    /// <summary>Salinan yang dapat diubah.</summary>
    public IsoSpecBuilder ToBuilder()
    {
        var builder = new IsoSpecBuilder
        {
            MtiEncoding = MtiEncoding,
            BitmapEncoding = BitmapEncoding,
            LengthEncoding = LengthEncoding,
            TpduLength = TpduLength,
        };
        foreach (IsoFieldSpec field in _fields.Values) builder.Fields[field.Number] = field;
        return builder;
    }
}

/// <summary>Builder <see cref="IsoSpec"/>.</summary>
public sealed class IsoSpecBuilder
{
    internal Dictionary<int, IsoFieldSpec> Fields { get; } = [];

    /// <summary>Encoding MTI (default ASCII).</summary>
    public IsoEncoding MtiEncoding { get; set; }

    /// <summary>Encoding bitmap (default ASCII hex).</summary>
    public IsoEncoding BitmapEncoding { get; set; }

    /// <summary>Encoding indikator panjang (default ASCII).</summary>
    public IsoEncoding LengthEncoding { get; set; }

    /// <summary>Panjang TPDU dalam byte (default 0).</summary>
    public int TpduLength { get; set; }

    /// <summary>Menambah atau mengganti definisi field.</summary>
    /// <param name="number">Nomor field 2–128.</param>
    /// <param name="lengthType">Tetap / LLVAR / dst.</param>
    /// <param name="content">n / an / ans.</param>
    /// <param name="length">Panjang tetap atau maksimum.</param>
    /// <param name="name">Nama untuk trace.</param>
    /// <param name="encoding">Encoding data (default ASCII).</param>
    /// <param name="padRight">BCD ganjil dipad kanan; default <c>true</c> hanya untuk field 35.</param>
    /// <param name="lengthUnit">Satuan indikator panjang BCD (lihat <see cref="IsoFieldSpec.LengthUnit"/>).</param>
    public IsoSpecBuilder Field(int number, IsoLengthType lengthType, IsoFieldContent content, int length, string name,
        IsoFieldEncoding encoding = IsoFieldEncoding.Ascii, bool? padRight = null, IsoLengthUnit? lengthUnit = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(number, 2);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(number, 128);
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 1);
        if (lengthType != IsoLengthType.Fixed && length > MaxForDigits((int)lengthType))
        {
            throw new ArgumentOutOfRangeException(nameof(length), $"Field {number}: panjang {length} tidak muat di indikator {(int)lengthType} digit.");
        }

        Fields[number] = new IsoFieldSpec(number, encoding, lengthType, content, length, name ?? string.Empty)
        {
            PadRight = padRight ?? (number == 35 && encoding == IsoFieldEncoding.Bcd),
            LengthUnit = lengthUnit,
        };
        return this;
    }

    /// <summary>Menghapus definisi field.</summary>
    public IsoSpecBuilder Remove(int number)
    {
        Fields.Remove(number);
        return this;
    }

    /// <summary>Membuat spesifikasi immutable.</summary>
    public IsoSpec Build()
    {
        ArgumentOutOfRangeException.ThrowIfNegative(TpduLength);
        return new IsoSpec(this);
    }

    internal static int MaxForDigits(int digits) => digits >= 6 ? 999_999 : (int)Math.Pow(10, digits) - 1;
}
