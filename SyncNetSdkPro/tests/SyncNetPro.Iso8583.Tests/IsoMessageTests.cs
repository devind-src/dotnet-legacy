using System.Text;
using Newtonsoft.Json.Linq;
using SyncNetPro.Contracts;

namespace SyncNetPro.Iso8583.Tests;

public class IsoMessageTests
{
    private static readonly IsoSpec Biller = IsoSpec.Legacy.ToBuilder()
        .Field(7, IsoLengthType.Fixed, IsoFieldContent.N, 10, "Transmission Date Time")
        .Field(11, IsoLengthType.Fixed, IsoFieldContent.N, 6, "STAN")
        .Field(39, IsoLengthType.Fixed, IsoFieldContent.An, 2, "Response Code")
        .Field(41, IsoLengthType.Fixed, IsoFieldContent.Ans, 8, "Terminal ID")
        .Build();

    private static IsoMessage Sample() => new IsoMessage(Biller, "0200")
        .Set(2, "4111111111111111")
        .Set(3, "380000")
        .Set(11, "000123")
        .Set(41, "TERM0001")
        .Set(52, "A1B2C3D4E5F60718");

    [Fact]
    public void Round_trip_keeps_every_field()
    {
        IsoMessage original = Sample().Set(103, "532110000001");

        IsoMessage parsed = IsoMessage.Parse(Biller, original.Pack());

        Assert.Equal("0200", parsed.Mti);
        Assert.Equal(original.Fields, parsed.Fields);
        Assert.Null(parsed[4]);
        Assert.True(parsed.IsRequest);
    }

    [Fact]
    public void Undefined_field_is_rejected_when_set()
    {
        IsoSpec spec = IsoSpec.Legacy.ToBuilder().Remove(70).Build();

        IsoFormatException ex = Assert.Throws<IsoFormatException>(() => new IsoMessage(spec, "0800").Set(70, "301"));

        Assert.Equal(70, ex.Field);
        Assert.Contains("IsoSpecBuilder.Field(70", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Field_present_in_bitmap_but_not_in_spec_fails_parse_with_field_number()
    {
        byte[] packed = new IsoMessage(IsoSpec.Legacy, "0800").Set(11, "000000000001").Set(70, "301").Pack();
        IsoSpec without70 = IsoSpec.Legacy.ToBuilder().Remove(70).Build();

        IsoFormatException ex = Assert.Throws<IsoFormatException>(() => IsoMessage.Parse(without70, packed));

        Assert.Equal(70, ex.Field);
    }

    [Theory]
    [InlineData(11, "00012A", "numerik")]
    [InlineData(11, "0001234", "tepat 6")]
    [InlineData(2, "41111111111111111111", "maksimal 19")]
    [InlineData(48, "Rp €", "Latin-1")]
    public void Invalid_values_are_rejected_with_a_clear_message(int field, string value, string expected)
    {
        IsoFormatException ex = Assert.Throws<IsoFormatException>(() => new IsoMessage(Biller, "0200").Set(field, value));

        Assert.Equal(field, ex.Field);
        Assert.Contains(expected, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Masked_pan_is_allowed_like_legacy_pci_rule()
    {
        IsoMessage message = new IsoMessage(Biller, "0200").Set(2, "411111******1111");

        Assert.Equal("411111******1111", IsoMessage.Parse(Biller, message.Pack())[2]);
    }

    [Theory]
    [InlineData("020")]
    [InlineData("02X0")]
    public void Mti_must_be_four_digits(string mti) =>
        Assert.Throws<IsoFormatException>(() => new IsoMessage(Biller, mti));

    [Fact]
    public void Pack_without_mti_fails() =>
        Assert.Throws<IsoFormatException>(() => new IsoMessage(Biller).Set(11, "000001").Pack());

    [Fact]
    public void Truncated_message_reports_position()
    {
        byte[] packed = Sample().Pack();

        IsoFormatException ex = Assert.Throws<IsoFormatException>(() => IsoMessage.Parse(Biller, packed.AsSpan(0, packed.Length - 5)));

        Assert.Equal(52, ex.Field);
        Assert.Contains("terpotong", ex.Message, StringComparison.Ordinal);
        Assert.False(IsoMessage.TryParse(Biller, packed.AsSpan(0, 10), out IsoMessage? none, out string? error));
        Assert.Null(none);
        Assert.NotNull(error);
    }

    [Fact]
    public void Non_numeric_length_indicator_fails()
    {
        byte[] packed = Sample().Pack();
        int panLength = Encoding.ASCII.GetString(packed).IndexOf("16411111", StringComparison.Ordinal);
        packed[panLength] = (byte)'X';

        IsoFormatException ex = Assert.Throws<IsoFormatException>(() => IsoMessage.Parse(Biller, packed));

        Assert.Equal(2, ex.Field);
        Assert.Equal(panLength, ex.Offset);
    }

    [Fact]
    public void Response_uses_response_mti_and_response_code()
    {
        IsoMessage response = Sample().CreateResponse("00");

        Assert.Equal("0210", response.Mti);
        Assert.Equal("00", response[39]);
        Assert.Equal("000123", response[11]);
        Assert.True(response.IsResponse);
        Assert.Null(Sample()[39]);
    }

    [Fact]
    public void Trace_masks_pan_pin_block_and_track_data_by_default()
    {
        IsoMessage message = Sample().Set(35, "4111111111111111=2512101");

        string trace = message.FormatTrace();

        Assert.Contains("[411111******1111]", trace, StringComparison.Ordinal);
        Assert.Contains("052  [****************]", trace, StringComparison.Ordinal);
        Assert.Contains("035  [************************]", trace, StringComparison.Ordinal);
        Assert.DoesNotContain("A1B2C3D4", trace, StringComparison.Ordinal);
        Assert.DoesNotContain("41 31 42 32", trace, StringComparison.Ordinal); // PIN block di hex dump
        Assert.DoesNotContain("=2512", trace, StringComparison.Ordinal);
        Assert.Contains("4111111111111111", message.FormatTrace(IsoFormatOptions.None), StringComparison.Ordinal);
    }

    [Fact]
    public void Trace_of_unparseable_bytes_hides_everything_after_the_error()
    {
        byte[] packed = Sample().Pack();
        int stan = Encoding.ASCII.GetString(packed).IndexOf("000123", StringComparison.Ordinal);
        packed[stan] = (byte)'X';

        string trace = IsoMessage.FormatTrace(Biller, packed);

        Assert.Contains("Field 11", trace, StringComparison.Ordinal);
        Assert.Contains("byte berikutnya tidak ditampilkan", trace, StringComparison.Ordinal);
        Assert.DoesNotContain("TERM0001", trace, StringComparison.Ordinal);
        Assert.DoesNotContain("1111111111", trace, StringComparison.Ordinal);
    }

    [Fact]
    public void Bcd_byte_length_field_requires_even_digits()
    {
        IsoSpec spec = IsoSpec.Create(s =>
        {
            s.LengthEncoding = IsoEncoding.Ascii;
            s.Field(2, IsoLengthType.LLVar, IsoFieldContent.N, 19, "PAN", IsoFieldEncoding.Bcd);
        });

        IsoFormatException ex = Assert.Throws<IsoFormatException>(() => new IsoMessage(spec, "0200").Set(2, "411111111111111"));
        Assert.Contains("IsoLengthUnit.Characters", ex.Message, StringComparison.Ordinal);

        IsoSpec digits = spec.ToBuilder().Field(2, IsoLengthType.LLVar, IsoFieldContent.N, 19, "PAN", IsoFieldEncoding.Bcd, lengthUnit: IsoLengthUnit.Characters).Build();
        IsoMessage odd = new IsoMessage(digits, "0200").Set(2, "411111111111111");
        Assert.Equal("411111111111111", IsoMessage.Parse(digits, odd.Pack())[2]);
    }

    [Fact]
    public void Tpdu_must_match_spec_length()
    {
        IsoSpec spec = IsoSpec.Legacy.ToBuilder().Build();
        IsoSpecBuilder withTpdu = spec.ToBuilder();
        withTpdu.TpduLength = 5;
        IsoSpec tpdu = withTpdu.Build();

        Assert.Throws<IsoFormatException>(() => new IsoMessage(tpdu, "0800") { Tpdu = "6000" });
        Assert.Throws<IsoFormatException>(() => new IsoMessage(tpdu, "0800").Set(11, "000000000001").Pack());
        Assert.Equal("6000010000", IsoMessage.Parse(tpdu, new IsoMessage(tpdu, "0800") { Tpdu = "6000010000" }.Set(11, "000000000001").Pack()).Tpdu);
    }

    [Fact]
    public void Legacy_spec_is_immutable()
    {
        IsoSpecBuilder builder = IsoSpec.Legacy.ToBuilder().Field(11, IsoLengthType.Fixed, IsoFieldContent.N, 6, "STAN");

        Assert.Equal(12, IsoSpec.Legacy.GetField(11)!.Length);
        Assert.Equal(6, builder.Build().GetField(11)!.Length);
    }

    [Fact]
    public void Field_one_is_the_secondary_bitmap_and_cannot_be_set() =>
        Assert.Throws<IsoFormatException>(() => new IsoMessage(Biller, "0200").Set(1, "0000000000000000"));

    [Fact]
    public void Legacy_trace_writes_bitmaps_as_fields_0_and_1()
    {
        string format = Sample().Set(100, "12345").Format();

        Assert.StartsWith("0200\n[Fixed    an       16 016] 000  [", format, StringComparison.Ordinal);
        Assert.Contains("] 001  [", format, StringComparison.Ordinal);
    }
}

public class ProcessingCodeTests
{
    private static readonly JObject Golden = JObject.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Golden", "processing-code.json")));

    [Fact]
    public void Tran_type_from_processing_code_matches_legacy()
    {
        foreach (JObject c in Golden["toTranType"]!.Cast<JObject>())
        {
            Assert.Equal((string)c["tranType"]!, ProcessingCode.ToTranType((string)c["mti"]!, (string)c["pcode"]!));
        }
    }

    [Fact]
    public void Processing_code_from_tran_type_matches_legacy()
    {
        foreach (JObject c in Golden["fromTranType"]!.Cast<JObject>())
        {
            Assert.Equal((string)c["pcode"]!, ProcessingCode.FromTranType((string)c["tranType"]!, (string?)c["ext"], "10", "20"));
        }
    }

    [Fact]
    public void Missing_account_types_default_to_00_instead_of_a_short_code()
    {
        Assert.Equal("500000", ProcessingCode.FromTranType(TranType.Payment));
        Assert.Equal("00", ProcessingCode.FromAccountType("50"));
        Assert.Equal("20", ProcessingCode.ToAccountType("501020"));
    }
}
