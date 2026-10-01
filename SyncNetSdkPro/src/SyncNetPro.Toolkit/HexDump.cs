using System.Globalization;
using System.Text;

namespace SyncNetPro.Toolkit;

/// <summary>Format biner untuk trace, tata letak sama dengan <c>NbFormat.FormatBinary</c> SDK lama.</summary>
public static class HexDump
{
    private const int RowLength = 16;

    /// <summary>
    /// Baris <c>[00000]  teks-16-karakter  hex hex …</c>. Baris diakhiri <c>\n</c> di semua OS
    /// (SDK lama: <c>\r\n</c>, dan menambah baris kosong bila panjang kelipatan 16).
    /// </summary>
    public static string Format(ReadOnlySpan<byte> bytes)
    {
        var sb = new StringBuilder();
        for (int offset = 0; offset < bytes.Length || offset == 0; offset += RowLength)
        {
            ReadOnlySpan<byte> row = bytes.Slice(offset, Math.Min(RowLength, bytes.Length - offset));
            sb.Append('[').Append(offset.ToString("D5", CultureInfo.InvariantCulture)).Append("]  ");
            foreach (byte b in row) sb.Append(Printable(b));
            sb.Append(' ', RowLength - row.Length).Append("  ");
            foreach (byte b in row) sb.Append(' ').Append(b.ToString("X2", CultureInfo.InvariantCulture));
            sb.Append('\n');
            if (bytes.Length == 0) break;
        }

        return sb.ToString();
    }

    /// <summary>Karakter di luar 32–126 diganti <c>.</c> (sama dengan <c>NbFormat.FormatString</c>).</summary>
    public static string Printable(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return string.Create(text.Length, text, static (span, s) =>
        {
            for (int i = 0; i < s.Length; i++) span[i] = s[i] is >= ' ' and <= '~' ? s[i] : '.';
        });
    }

    private static char Printable(byte b) => b is >= 32 and <= 126 ? (char)b : '.';
}
