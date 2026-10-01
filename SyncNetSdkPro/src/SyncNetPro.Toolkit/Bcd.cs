namespace SyncNetPro.Toolkit;

/// <summary>
/// Binary Coded Decimal (packed): dua digit per byte, mis. <c>"0200"</c> ↔ <c>02 00</c>.
/// Digit hex <c>A–F</c> diterima (dipakai track 2 <c>D</c>/<c>F</c>).
/// </summary>
public static class Bcd
{
    /// <summary>Jumlah byte untuk <paramref name="digits"/> digit.</summary>
    public static int ByteCount(int digits) => (digits + 1) / 2;

    /// <summary>
    /// Encode digit ke BCD. Jumlah digit ganjil dipad <c>0</c> di kiri (default) atau kanan
    /// (<paramref name="padRight"/>, mis. track 2).
    /// </summary>
    /// <exception cref="FormatException">Ada karakter selain 0–9/A–F.</exception>
    public static byte[] Encode(string digits, bool padRight = false)
    {
        ArgumentNullException.ThrowIfNull(digits);
        if (digits.Length % 2 != 0) digits = padRight ? digits + "0" : "0" + digits;
        return Convert.FromHexString(digits);
    }

    /// <summary>Decode BCD menjadi digit (huruf besar untuk A–F), 2 digit per byte.</summary>
    public static string Decode(ReadOnlySpan<byte> bytes) => Convert.ToHexString(bytes);

    /// <summary>
    /// Decode BCD lalu buang digit pad bila <paramref name="digits"/> ganjil
    /// (pad kiri: digit pertama; pad kanan: digit terakhir).
    /// </summary>
    public static string Decode(ReadOnlySpan<byte> bytes, int digits, bool padRight = false)
    {
        string all = Convert.ToHexString(bytes);
        if (all.Length == digits) return all;
        return padRight ? all[..digits] : all[^digits..];
    }
}
