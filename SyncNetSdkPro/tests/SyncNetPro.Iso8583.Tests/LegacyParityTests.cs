using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SyncNetPro.Toolkit;

namespace SyncNetPro.Iso8583.Tests;

/// <summary>SDK baru identik dengan <c>SyncNet.IsoMessage.Iso8583</c> lama, kecuali bug yang terdokumentasi.</summary>
public partial class LegacyParityTests
{
    public static TheoryData<string> ValidCases => GoldenCases.Names(c => (string)c["name"]! != "ascii-fixed-short");

    public static TheoryData<string> LegacyUnpackOk => GoldenCases.Names(c =>
        (int)c["legacyUnpack"]!["rc"]! == 0 && !GoldenCases.LegacyCorruptUnpack.Contains((string)c["name"]!));

    public static TheoryData<string> LegacyUnpackBroken => GoldenCases.Names(c =>
        (string)c["name"]! != "ascii-fixed-short"
        && ((int)c["legacyUnpack"]!["rc"]! != 0 || GoldenCases.LegacyCorruptUnpack.Contains((string)c["name"]!)));

    [Theory]
    [MemberData(nameof(ValidCases))]
    public void Pack_is_byte_identical_to_legacy(string name)
    {
        JObject c = GoldenCases.Get(name);

        byte[] packed = GoldenCases.Message(c).Pack();

        Assert.Equal((string)c["packed"]!, Convert.ToHexString(packed));
    }

    [Theory]
    [MemberData(nameof(ValidCases))]
    public void Trace_text_matches_legacy(string name)
    {
        JObject c = GoldenCases.Get(name);
        IsoMessage message = GoldenCases.Message(c);

        Assert.Equal((string)c["format"]!, message.Format(IsoFormatOptions.None));
        Assert.Equal((string)c["simple"]!, message.FormatSimple(IsoFormatOptions.None));
        Assert.Equal(WithoutLegacyEmptyRow((string)c["dump"]!), HexDump.Format(Convert.FromHexString((string)c["packed"]!)));
    }

    [Theory]
    [MemberData(nameof(ValidCases))]
    public void Parse_of_legacy_bytes_returns_the_original_fields(string name)
    {
        JObject c = GoldenCases.Get(name);

        IsoMessage parsed = IsoMessage.Parse(GoldenCases.Spec(c), Convert.FromHexString((string)c["packed"]!));

        Assert.Equal((string)c["mti"]!, parsed.Mti);
        string tpdu = (string)c["tpdu"]!;
        Assert.Equal(tpdu.Length == 0 ? null : tpdu, parsed.Tpdu);
        Assert.Equal(GoldenCases.Fields(c["fields"]!), parsed.Fields.ToDictionary());
    }

    [Theory]
    [MemberData(nameof(LegacyUnpackOk))]
    public void Parse_matches_legacy_unpack_where_legacy_was_correct(string name)
    {
        JObject c = GoldenCases.Get(name);

        IsoMessage parsed = IsoMessage.Parse(GoldenCases.Spec(c), Convert.FromHexString((string)c["packed"]!));

        Assert.Equal(GoldenCases.Fields(c["legacyUnpack"]!["fields"]!), parsed.Fields.ToDictionary());
    }

    [Theory]
    [MemberData(nameof(LegacyUnpackBroken))]
    public void Cases_the_legacy_sdk_could_not_unpack_now_round_trip(string name)
    {
        // SDK lama: gagal (rc != 0) atau hasilnya rusak — BCD >= 0x80 via UTF-8, track 2 ganjil, offset MTI BCD + bitmap ASCII,
        // BCD dengan indikator panjang ASCII, bitmap BCD sekunder.
        JObject c = GoldenCases.Get(name);
        Assert.NotEqual(GoldenCases.Fields(c["fields"]!), GoldenCases.Fields(c["legacyUnpack"]!["fields"]!));

        IsoMessage parsed = IsoMessage.Parse(GoldenCases.Spec(c), Convert.FromHexString((string)c["packed"]!));

        Assert.Equal(GoldenCases.Fields(c["fields"]!), parsed.Fields.ToDictionary());
    }

    [Fact]
    public void Fixed_field_with_wrong_length_is_rejected_instead_of_corrupting_the_message()
    {
        JObject c = GoldenCases.Get("ascii-fixed-short");
        Assert.NotEqual(0, (int)c["legacyUnpack"]!["rc"]!);

        var message = new IsoMessage(IsoSpec.Legacy, "0200");
        IsoFormatException set = Assert.Throws<IsoFormatException>(() => message[3] = "38");
        Assert.Equal(3, set.Field);
        Assert.Contains("tepat 6", set.Message, StringComparison.Ordinal);

        IsoFormatException parse = Assert.Throws<IsoFormatException>(() => IsoMessage.Parse(IsoSpec.Legacy, Convert.FromHexString((string)c["packed"]!)));
        Assert.NotNull(parse.Field);
    }

    // NbFormat.FormatBinary lama menambah baris kosong bila panjang kelipatan 16.
    private static string WithoutLegacyEmptyRow(string dump) => EmptyRow().Replace(dump, string.Empty);

    [GeneratedRegex(@"\[\d{5}\] {18}  \n$")]
    private static partial Regex EmptyRow();
}
