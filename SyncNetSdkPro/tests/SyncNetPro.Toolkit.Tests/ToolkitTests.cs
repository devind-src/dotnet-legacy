using System.Text;
using Newtonsoft.Json.Linq;

namespace SyncNetPro.Toolkit.Tests;

/// <summary>Paritas dengan SDK lama (tools/SyncNetPro.GoldenGenerator/ToolkitGolden.cs) + perbaikan bug.</summary>
public class ToolkitTests
{
    private static readonly JObject Golden = JObject.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Golden", "toolkit.json")));

    [Fact]
    public void Des_and_triple_des_match_legacy()
    {
        foreach (JObject c in Golden["des"]!.Cast<JObject>())
        {
            string key = (string)c["key"]!;
            Assert.Equal((string)c["encrypted"]!, DesEcb.EncryptHex((string)c["block"]!, key));
            Assert.Equal((string)c["block"]!, DesEcb.DecryptHex((string)c["encrypted"]!, key));
            Assert.Equal((string)c["kcv"]!, KeyCheckValue.Compute(key));
        }
    }

    [Theory]
    [InlineData("0123456789ABCD")]
    [InlineData("0123456789ABCDEF01")]
    public void Wrong_key_length_throws_instead_of_returning_zero_block(string key) =>
        Assert.Throws<ArgumentException>(() => DesEcb.EncryptHex("0000000000000000", key));

    [Fact]
    public void Pin_blocks_match_legacy()
    {
        foreach (JObject c in Golden["pins"]!.Cast<JObject>())
        {
            var format = (PinBlockFormat)(int)c["format"]!;
            string pan = (string)c["pan"]!, pin = (string)c["pin"]!, key = (string)c["key"]!;
            Assert.Equal((string)c["clear"]!, PinBlock.CreateClear(pan, pin, format));
            Assert.Equal((string)c["block"]!, PinBlock.Create(pan, pin, key, format));
        }
    }

    [Fact]
    public void Iso0_uses_rightmost_12_digits_excluding_check_digit_for_long_pans()
    {
        // ISO 9564 format 0: PIN field 041234FFFFFFFFFF XOR 0000 + 12 digit PAN sebelum check digit.
        Assert.Equal("041225EEEEEEEEEE", PinBlock.CreateClear("4111111111111111", "1234", PinBlockFormat.Iso0));
        Assert.Equal("041200A9876FEDCB", PinBlock.CreateClear("6220123456789012345", "1234", PinBlockFormat.Iso0));
        // PIN 12 digit: panjang ditulis hex (C); SDK lama menulis "012" sehingga blok bergeser.
        Assert.Equal("0C123456789012FF", PinBlock.CreateClear("4000000000000000002", "123456789012", PinBlockFormat.Iso0));
    }

    [Theory]
    [InlineData("4111111111111111", "12a4", PinBlockFormat.Iso0)]
    [InlineData("4111111111111111", "123", PinBlockFormat.Iso0)]
    [InlineData("41111111111", "1234", PinBlockFormat.Iso0)]
    [InlineData("4111111111111111", "1234567", PinBlockFormat.Docutel)]
    public void Invalid_pin_or_pan_throws_instead_of_encrypting_anyway(string pan, string pin, PinBlockFormat format) =>
        Assert.Throws<ArgumentException>(() => PinBlock.Create(pan, pin, "0123456789ABCDEFFEDCBA9876543210", format));

    [Fact]
    public void Luhn_matches_legacy_and_accepts_12_to_19_digits()
    {
        foreach (JObject c in Golden["luhn"]!.Cast<JObject>())
        {
            string digits = (string)c["digits"]!;
            Assert.Equal((int)c["check"]!, Luhn.CheckDigit(digits));
            Assert.Equal((string)c["full"]!, Luhn.Append(digits));
            Assert.True(Luhn.IsValid((string)c["full"]!)); // SDK lama: salah untuk 12 dan 19 digit
        }

        Assert.False(Luhn.IsValid("4111111111111112"));
        Assert.False(Luhn.IsValid("41111111111a1111"));
    }

    [Fact]
    public void Emv_tlv_matches_legacy()
    {
        foreach (JObject c in Golden["emv"]!.Cast<JObject>())
        {
            IReadOnlyList<EmvTlvItem> items = EmvTlv.Parse((string)c["hex"]!);
            Assert.Equal(
                c["items"]!.Select(i => ((string)i["tag"]!, (string)i["value"]!)),
                items.Select(i => (i.Tag, i.ValueHex)));
            Assert.Equal((string)c["rebuilt"]!, Convert.ToHexString(EmvTlv.Build(items)));
            Assert.Equal((string)c["info"]!, EmvTlv.Describe(items));
        }
    }

    [Fact]
    public void Emv_tlv_keeps_repeated_tags_and_rejects_truncated_data()
    {
        IReadOnlyList<EmvTlvItem> items = EmvTlv.Parse("7203860100" + "7203860101");
        Assert.Equal(2, items.Count);

        Assert.Throws<FormatException>(() => EmvTlv.Parse("9F2608A1B2"));
        Assert.Throws<FormatException>(() => EmvTlv.Parse("9F"));
        Assert.Throws<FormatException>(() => EmvTlv.Parse("5A80"));
    }

    [Fact]
    public void Numeric_tlv_matches_legacy_and_rejects_truncated_data()
    {
        JObject c = (JObject)Golden["numeric"]!;
        var elements = new Dictionary<int, string?> { [1] = "HELLO", [3] = "", [26] = "ID.CO.QRIS.WWW", [39] = "X" };
        var three = new NumericTlv();
        var two = new NumericTlv(2, 2);

        Assert.Equal((string)c["build3"]!, three.Build(elements));
        Assert.Equal((string)c["build2"]!, two.Build(elements));
        Assert.Equal((string)c["element"]!, three.Element(7, "ABC"));
        Assert.Equal(Map(c["parse3"]!), three.Parse((string)c["build3"]!));
        Assert.Equal(Map(c["parse2"]!), two.Parse((string)c["build2"]!));

        // SDK lama mengembalikan hasil sebagian ({1: HELLO}) tanpa error.
        Assert.Throws<FormatException>(() => three.Parse("001005HELLO002010ABC"));
        Assert.False(three.TryParse("00A005HELLO", out _));
    }

    [Fact]
    public void Hex_dump_matches_legacy_without_trailing_empty_row()
    {
        foreach (JObject c in Golden["dumps"]!.Cast<JObject>())
        {
            byte[] bytes = Convert.FromHexString((string)c["hex"]!);
            string legacy = (string)c["dump"]!;
            if (bytes.Length % 16 == 0) legacy = legacy[..legacy.LastIndexOf('[')];
            if (bytes.Length == 0) legacy = "[00000]                    \n";
            Assert.Equal(legacy, HexDump.Format(bytes));
        }
    }

    [Fact]
    public void Bcd_and_ebcdic_match_legacy()
    {
        foreach (JObject c in Golden["bcd"]!.Cast<JObject>())
        {
            string digits = (string)c["digits"]!;
            bool padRight = (bool)c["padRight"]!;
            Assert.Equal((string)c["hex"]!, Convert.ToHexString(Bcd.Encode(digits, padRight)));
            Assert.Equal(digits, Bcd.Decode(Convert.FromHexString((string)c["hex"]!), digits.Length, padRight));
        }

        JObject e = (JObject)Golden["ebcdic"]!;
        Assert.Equal((string)e["hex"]!, Convert.ToHexString(Ebcdic.Encode((string)e["text"]!)));
        Assert.Equal((string)e["text"]!, Ebcdic.Decode(Convert.FromHexString((string)e["hex"]!)));
    }

    [Fact]
    public void Printable_replaces_control_and_non_ascii() =>
        Assert.Equal("A.B.", HexDump.Printable("A\u0001Bé"));

    private static Dictionary<int, string> Map(JToken token) =>
        ((JObject)token).Properties().ToDictionary(p => int.Parse(p.Name, System.Globalization.CultureInfo.InvariantCulture), p => (string)p.Value!);
}
