using System.Text;

namespace SyncNetApi.Cryptography
{
    /// <summary>Port of legacy Cryptography/DesAlgorithm.EncryptText/DecryptText — chunks an
    /// arbitrary-length UTF-8 string into 8-byte DES blocks (hex string padded with 'F' to a
    /// multiple of 16 hex chars), encrypts each block via HsmDesCipher.EncryptHex under the
    /// given key, and prepends a 1-byte header recording how many padding bytes were added (so
    /// DecryptText can strip them back off). Backs Tools &gt; Encrypt Credential's "XML" mode
    /// (legacy NbXml.EncryptValue). DecryptText is ported alongside for symmetry/testability
    /// even though no Tool currently exposes a decrypt button for this mode.</summary>
    public static class LegacyDesTextCipher
    {
        public static string EncryptText(string plainText, string keyHex)
        {
            var hex = Convert.ToHexString(Encoding.UTF8.GetBytes(plainText));

            var remainder = hex.Length % 16;
            var padded = remainder > 0 ? hex + new string('F', 16 - remainder) : hex;

            var sb = new StringBuilder();
            for (var i = 0; i < padded.Length; i += 16)
                sb.Append(HsmDesCipher.EncryptHex(padded.Substring(i, 16), keyHex));

            var paddingBytes = remainder > 0 ? (16 - remainder) / 2 : 0;
            return paddingBytes.ToString("x2") + sb;
        }

        public static string DecryptText(string encryptedHex, string keyHex)
        {
            var paddingBytes = Convert.FromHexString(encryptedHex[..2])[0];
            var body = encryptedHex[2..];

            var sb = new StringBuilder();
            for (var i = 0; i < body.Length; i += 16)
                sb.Append(HsmDesCipher.DecryptHex(body.Substring(i, 16), keyHex));

            var resultHex = sb.ToString();
            resultHex = resultHex[..^(paddingBytes * 2)];

            return Encoding.UTF8.GetString(Convert.FromHexString(resultHex));
        }
    }
}
