using System.Globalization;
using System.Text;

namespace SyncNetPro.Toolkit;

/// <summary>
/// TLV teks dengan tag dan panjang numerik (default 3 + 3 digit, <c>NbTlvQris</c> SDK lama),
/// mis. <c>001005HELLO</c>. Untuk payload QR EMVCo gunakan <c>new NumericTlv(2, 2)</c>.
/// </summary>
public sealed class NumericTlv(int tagDigits = 3, int lengthDigits = 3)
{
    /// <summary>Jumlah digit tag.</summary>
    public int TagDigits { get; } = tagDigits is >= 1 and <= 6 ? tagDigits : throw new ArgumentOutOfRangeException(nameof(tagDigits));

    /// <summary>Jumlah digit panjang.</summary>
    public int LengthDigits { get; } = lengthDigits is >= 1 and <= 6 ? lengthDigits : throw new ArgumentOutOfRangeException(nameof(lengthDigits));

    /// <summary>Satu elemen <c>tag + panjang + nilai</c>.</summary>
    public string Element(int tag, string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        string t = tag.ToString(CultureInfo.InvariantCulture).PadLeft(TagDigits, '0');
        string l = value.Length.ToString(CultureInfo.InvariantCulture).PadLeft(LengthDigits, '0');
        if (tag < 0 || t.Length > TagDigits) throw new ArgumentOutOfRangeException(nameof(tag), $"Tag {tag} tidak muat di {TagDigits} digit.");
        if (l.Length > LengthDigits) throw new ArgumentException($"Nilai tag {tag} terlalu panjang untuk {LengthDigits} digit panjang.", nameof(value));
        return t + l + value;
    }

    /// <summary>Menyusun TLV dari pasangan tag/nilai (nilai kosong dilewati), urut sesuai input.</summary>
    public string Build(IEnumerable<KeyValuePair<int, string?>> elements)
    {
        ArgumentNullException.ThrowIfNull(elements);
        var sb = new StringBuilder();
        foreach ((int tag, string? value) in elements)
        {
            if (!string.IsNullOrEmpty(value)) sb.Append(Element(tag, value));
        }

        return sb.ToString();
    }

    /// <summary>Parse ke tag → nilai (urut kemunculan).</summary>
    /// <exception cref="FormatException">Tag/panjang bukan angka atau data terpotong (SDK lama mengembalikan hasil sebagian).</exception>
    public IReadOnlyDictionary<int, string> Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var result = new Dictionary<int, string>();
        int offset = 0;
        while (offset < text.Length)
        {
            int tag = Number(text, offset, TagDigits, "tag");
            int length = Number(text, offset + TagDigits, LengthDigits, $"panjang tag {tag}");
            int start = offset + TagDigits + LengthDigits;
            if (length > text.Length - start) throw new FormatException($"TLV terpotong: tag {tag} butuh {length} karakter di posisi {start}.");
            result[tag] = text.Substring(start, length);
            offset = start + length;
        }

        return result;
    }

    /// <summary>Seperti <see cref="Parse"/> tanpa exception.</summary>
    public bool TryParse(string? text, out IReadOnlyDictionary<int, string> elements)
    {
        try
        {
            elements = Parse(text ?? string.Empty);
            return text is not null && text.Length > 0;
        }
        catch (FormatException)
        {
            elements = new Dictionary<int, string>();
            return false;
        }
    }

    private static int Number(string text, int offset, int digits, string what)
    {
        if (digits > text.Length - offset) throw new FormatException($"TLV terpotong saat membaca {what} di posisi {offset}.");
        ReadOnlySpan<char> span = text.AsSpan(offset, digits);
        return span.ContainsAnyExceptInRange('0', '9')
            ? throw new FormatException($"{what} di posisi {offset} bukan angka: '{span}'.")
            : int.Parse(span, NumberStyles.None, CultureInfo.InvariantCulture);
    }
}
