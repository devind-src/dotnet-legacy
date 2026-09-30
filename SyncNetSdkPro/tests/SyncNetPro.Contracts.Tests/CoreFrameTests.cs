namespace SyncNetPro.Contracts.Tests;

public class CoreFrameTests
{
    [Fact]
    public void Encodes_two_byte_big_endian_length_excluding_header()
    {
        byte[] frame = CoreFrame.Encode(new byte[50]);

        Assert.Equal(52, frame.Length);
        Assert.Equal(0x00, frame[0]);
        Assert.Equal(0x32, frame[1]);
    }

    [Fact]
    public void Encodes_maximum_payload()
    {
        byte[] frame = CoreFrame.Encode(new byte[CoreFrame.MaxPayloadLength]);

        Assert.Equal(0xFF, frame[0]);
        Assert.Equal(0xFF, frame[1]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(CoreFrame.MaxPayloadLength + 1)]
    public void Rejects_invalid_payload_length(int length) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CoreFrame.Encode(new byte[length]));

    [Fact]
    public void Decodes_complete_frame_and_reports_consumed_bytes()
    {
        byte[] first = CoreFrame.Encode("{\"a\":1}"u8);
        byte[] second = CoreFrame.Encode("{}"u8);
        byte[] buffer = [.. first, .. second];

        Assert.True(CoreFrame.TryDecode(buffer, out ReadOnlySpan<byte> payload, out int consumed));
        Assert.Equal("{\"a\":1}"u8.ToArray(), payload.ToArray());
        Assert.Equal(first.Length, consumed);

        Assert.True(CoreFrame.TryDecode(buffer.AsSpan(consumed), out payload, out _));
        Assert.Equal("{}"u8.ToArray(), payload.ToArray());
    }

    [Fact]
    public void Returns_false_for_incomplete_frame()
    {
        byte[] frame = CoreFrame.Encode("{\"a\":1}"u8);

        Assert.False(CoreFrame.TryDecode(frame.AsSpan(0, 1), out _, out _));
        Assert.False(CoreFrame.TryDecode(frame.AsSpan(0, frame.Length - 1), out _, out _));
    }

    [Fact]
    public void Rejects_zero_length_header() =>
        Assert.Throws<InvalidDataException>(() => CoreFrame.TryDecode(new byte[] { 0, 0, 1 }, out _, out _));

    [Fact]
    public void Non_ascii_payload_is_utf8()
    {
        var request = new CoreRequest().SetAdditionalData("name", "Śantoso 日本");

        byte[] frame = CoreMessageCodec.Default.EncodeFrame(request);
        CoreRequest decoded = CoreMessageCodec.Default.DecodePayload<CoreRequest>(frame.AsSpan(CoreFrame.HeaderLength));

        Assert.True(decoded.TryGetAdditionalData("name", out string? name));
        Assert.Equal("Śantoso 日本", name);
    }
}
