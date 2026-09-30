using System.Globalization;
using System.Text;

namespace SyncNetPro.Toolkit;

/// <summary>Satu elemen BER-TLV EMV (mis. isi field 55).</summary>
/// <param name="Tag">Tag hex, mis. <c>9F26</c>.</param>
/// <param name="Value">Nilai.</param>
public sealed record EmvTlvItem(string Tag, byte[] Value)
{
    /// <summary>Nilai dalam hex.</summary>
    public string ValueHex => Convert.ToHexString(Value);
}

/// <summary>BER-TLV EMV (<c>NbTlvEmv</c> SDK lama), level datar (tag konstruksi tidak diurai rekursif).</summary>
public static class EmvTlv
{
    private static readonly Dictionary<string, string> TagNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["4F"] = "Application Identifier(ADF Name)",
        ["57"] = "Track-2 Equivalent Data",
        ["5A"] = "Primary Account Number (PAN)",
        ["5F20"] = "Cardholder Name",
        ["5F24"] = "Application Expiration Date",
        ["5F25"] = "Application Effective Date",
        ["5F28"] = "Issuer Country Code",
        ["5F2A"] = "Transaction Currency Code",
        ["5F2D"] = "Language Preference",
        ["5F34"] = "Application Sequence Number",
        ["5F36"] = "Transaction Currency Exponent",
        ["5F50"] = "Issuer URL",
        ["71"] = "Issuer Script Data",
        ["72"] = "Issuer Script Data",
        ["82"] = "Application Interchange Profile",
        ["84"] = "Dedicated File (DF) Name",
        ["8A"] = "Authorization Response Code",
        ["8C"] = "Card Risk Management Data Object List 1 (CDOL1)",
        ["8D"] = "Card Risk Management Data Object List 2 (CDOL2)",
        ["91"] = "Issuer Authentication Data (ARPC + ARC)",
        ["95"] = "Terminal Verification Results (TVR)",
        ["9A"] = "Transaction Date",
        ["9C"] = "Transaction Type",
        ["9F02"] = "Amount, Authorized",
        ["9F03"] = "Amount, Other",
        ["9F06"] = "Application Identified (AID)",
        ["9F07"] = "Application Usage Control",
        ["9F08"] = "Application Version Number",
        ["9F09"] = "Application Version Number (Terminal)",
        ["9F0D"] = "Issuer Action Code - Default",
        ["9F0E"] = "Issuer Action Code - Denial",
        ["9F0F"] = "Issuer Action Code - Online",
        ["9F10"] = "Issuer Application Data",
        ["9F12"] = "Application Preferred Name",
        ["9F16"] = "Merchant Identifier",
        ["9F1A"] = "Terminal Country Code",
        ["9F1E"] = "Interface Device (IFD) Serial Number",
        ["9F26"] = "Application Request Cryptogram ARQC",
        ["9F27"] = "Cryptogram Information Data (CID)",
        ["9F33"] = "EMV Terminal Capabilities",
        ["9F34"] = "Cardholder Verification Method (CVM) Results",
        ["9F35"] = "Terminal Type",
        ["9F36"] = "Application Transaction Counter (ATC)",
        ["9F37"] = "Unpredictable Number",
        ["9F38"] = "Processing Options Data Object List (PDOL)",
        ["9F39"] = "Point of Service (POS) Entry Mode",
        ["9F40"] = "Additional Terminal Capabilities",
        ["9F41"] = "Transaction Sequence Counter",
        ["9F42"] = "Application Currency Code",
        ["9F44"] = "Application Currency Exponent",
        ["9F53"] = "Transaction Category Code",
        ["9F5B"] = "Issuer Script Results",
        ["DF01"] = "Kernel Identifier",
        ["9F1B"] = "Terminal Floor Limit",
        ["9F1D"] = "Terminal Risk Management Data",
        ["9F4E"] = "Merchant Name and Location",
        ["9F66"] = "Terminal Transaction Qualifiers (TTQ)",
        ["9F32"] = "Issuer Public Key Exponent",
        ["9F46"] = "ICC Public Key Certificate",
        ["9F47"] = "ICC Public Key Exponent",
        ["9F48"] = "ICC Public Key Remainder",
        ["9F49"] = "Dynamic Data Authentication Data Object List (DDOL)",
        ["9F4A"] = "Static Data Authentication Tag List",
        ["9F4B"] = "Signed Dynamic Application Data",
        ["9F4C"] = "ICC Dynamic Number",
        ["9F1C"] = "Terminal Identification",
        ["9F7C"] = "Merchant Custom Data",
        ["9F5D"] = "Token Requestor ID",
        ["9F6E"] = "e-Commerce Indicator (ECI)",
    };

    /// <summary>Nama tag (tabel NSICCS SDK lama), <c>Unknown Tag</c> bila tidak dikenal.</summary>
    public static string TagName(string tag) => TagNames.GetValueOrDefault(tag, "Unknown Tag");

    /// <summary>Parse hex. Urutan dan tag berulang dipertahankan (SDK lama gagal pada tag berulang).</summary>
    /// <exception cref="FormatException">Data terpotong atau panjang tidak valid.</exception>
    public static IReadOnlyList<EmvTlvItem> Parse(string hex) => Parse(Convert.FromHexString(hex));

    /// <summary>Parse byte.</summary>
    /// <exception cref="FormatException">Data terpotong atau panjang tidak valid.</exception>
    public static IReadOnlyList<EmvTlvItem> Parse(ReadOnlySpan<byte> data)
    {
        var items = new List<EmvTlvItem>();
        int i = 0;
        while (i < data.Length)
        {
            int tagStart = i;
            Need(data, i, 1, "tag");
            if ((data[i++] & 0x1F) == 0x1F)
            {
                do
                {
                    Need(data, i, 1, "tag");
                }
                while ((data[i++] & 0x80) == 0x80);
            }

            string tag = Convert.ToHexString(data[tagStart..i]);

            Need(data, i, 1, $"panjang tag {tag}");
            int length = data[i++];
            if (length >= 0x80)
            {
                int count = length & 0x7F;
                if (count is 0 or > 3) throw new FormatException($"Panjang tag {tag} tidak valid (0x{length:X2}).");
                Need(data, i, count, $"panjang tag {tag}");
                length = 0;
                for (int k = 0; k < count; k++) length = (length << 8) | data[i++];
            }

            Need(data, i, length, $"nilai tag {tag}");
            items.Add(new EmvTlvItem(tag, data.Slice(i, length).ToArray()));
            i += length;
        }

        return items;
    }

    /// <summary>Menyusun TLV.</summary>
    public static byte[] Build(IEnumerable<EmvTlvItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var output = new List<byte>();
        foreach (EmvTlvItem item in items)
        {
            output.AddRange(Convert.FromHexString(item.Tag));
            int length = item.Value.Length;
            if (length < 0x80)
            {
                output.Add((byte)length);
            }
            else
            {
                byte[] bytes = length <= 0xFF ? [(byte)length] : length <= 0xFFFF ? [(byte)(length >> 8), (byte)length] : [(byte)(length >> 16), (byte)(length >> 8), (byte)length];
                output.Add((byte)(0x80 | bytes.Length));
                output.AddRange(bytes);
            }

            output.AddRange(item.Value);
        }

        return [.. output];
    }

    /// <summary>Uraian untuk trace: <c>TAG  LL VALUE => nama</c> per baris (<c>NbTlvEmv.GetInfo</c>).</summary>
    public static string Describe(IEnumerable<EmvTlvItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var sb = new StringBuilder();
        foreach (EmvTlvItem item in items)
        {
            sb.Append(item.Tag.PadRight(4)).Append(' ')
                .Append(item.Value.Length.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0')).Append(' ')
                .Append(item.ValueHex).Append(" => ").Append(TagName(item.Tag)).Append('\n');
        }

        return sb.ToString();
    }

    private static void Need(ReadOnlySpan<byte> data, int offset, int count, string what)
    {
        if (count > data.Length - offset) throw new FormatException($"TLV terpotong saat membaca {what} di posisi {offset}.");
    }
}
