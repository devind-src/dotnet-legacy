namespace SyncNetPro.Iso8583;

/// <summary>Encoding MTI, bitmap, dan indikator panjang.</summary>
public enum IsoEncoding
{
    /// <summary>Teks ASCII (MTI 4 byte, bitmap 16 karakter hex, panjang "019").</summary>
    Ascii,

    /// <summary>BCD / biner (MTI 2 byte, bitmap 8 byte, panjang BCD).</summary>
    Bcd,
}

/// <summary>Encoding data field.</summary>
public enum IsoFieldEncoding
{
    /// <summary>Teks satu byte per karakter (Latin-1).</summary>
    Ascii,

    /// <summary>EBCDIC (IBM037).</summary>
    Ebcdic,

    /// <summary>BCD, dua digit per byte.</summary>
    Bcd,
}

/// <summary>Tipe panjang field: tetap atau variabel dengan 1–6 digit indikator panjang.</summary>
public enum IsoLengthType
{
    /// <summary>Panjang tetap.</summary>
    Fixed = 0,

    /// <summary>Indikator 1 digit.</summary>
    LVar = 1,

    /// <summary>Indikator 2 digit.</summary>
    LLVar = 2,

    /// <summary>Indikator 3 digit.</summary>
    LLLVar = 3,

    /// <summary>Indikator 4 digit.</summary>
    LLLLVar = 4,

    /// <summary>Indikator 5 digit.</summary>
    LLLLLVar = 5,

    /// <summary>Indikator 6 digit.</summary>
    LLLLLLVar = 6,
}

/// <summary>Isi field yang diizinkan.</summary>
public enum IsoFieldContent
{
    /// <summary>Numerik.</summary>
    N,

    /// <summary>Alfanumerik.</summary>
    An,

    /// <summary>Alfanumerik + spesial.</summary>
    Ans,
}

/// <summary>Satuan nilai indikator panjang untuk field BCD.</summary>
public enum IsoLengthUnit
{
    /// <summary>Jumlah digit/karakter nilai.</summary>
    Characters,

    /// <summary>Jumlah byte di wire (mis. ICC data field 55).</summary>
    Bytes,
}

/// <summary>Definisi satu data element.</summary>
/// <param name="Number">Nomor field 2–128.</param>
/// <param name="Encoding">Encoding data.</param>
/// <param name="LengthType">Tetap atau variabel.</param>
/// <param name="Content">Isi yang diizinkan.</param>
/// <param name="Length">Panjang tetap, atau panjang maksimum untuk field variabel.</param>
/// <param name="Name">Nama untuk trace.</param>
public sealed record IsoFieldSpec(int Number, IsoFieldEncoding Encoding, IsoLengthType LengthType, IsoFieldContent Content, int Length, string Name)
{
    /// <summary>
    /// Field BCD dengan jumlah digit ganjil: pad di kanan (mis. track 2) alih-alih kiri.
    /// Default <c>true</c> untuk field 35 BCD (perilaku SDK lama).
    /// </summary>
    public bool PadRight { get; init; }

    /// <summary>
    /// Satuan indikator panjang untuk field BCD variabel. <c>null</c> = default SDK lama:
    /// byte untuk field 55 atau bila indikator panjang ASCII; selain itu digit.
    /// </summary>
    public IsoLengthUnit? LengthUnit { get; init; }

    /// <summary>Jumlah digit indikator panjang (0 untuk field tetap).</summary>
    public int LengthDigits => (int)LengthType;
}
