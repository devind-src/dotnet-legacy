using System.Text;

namespace SyncNetPro.Toolkit;

/// <summary>EBCDIC (IBM037) — tersedia di semua OS tanpa registrasi manual code page.</summary>
public static class Ebcdic
{
    /// <summary>Encoding IBM037.</summary>
    public static Encoding Encoding { get; } = CreateEncoding();

    /// <summary>Teks → byte EBCDIC.</summary>
    public static byte[] Encode(string text) => Encoding.GetBytes(text);

    /// <summary>Byte EBCDIC → teks.</summary>
    public static string Decode(ReadOnlySpan<byte> bytes) => Encoding.GetString(bytes);

    private static Encoding CreateEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(37, EncoderFallback.ExceptionFallback, DecoderFallback.ReplacementFallback);
    }
}
