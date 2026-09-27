using System.Security.Cryptography;

namespace SyncNetApi.Cryptography
{
    /// <summary>Port of legacy Cryptography/DesAlgorithm.EncryptHex/DecryptHex — hex-in/hex-out
    /// DES (8-byte key) or Triple DES (16/24-byte key) in ECB mode, no padding, operating on
    /// whole 8-byte blocks. Only EncryptHex/DecryptHex are ported; legacy's EncryptText/
    /// DecryptText (arbitrary-length message chunking with a length header) belong to a
    /// different feature and are not needed for HSM key-ceremony math. Collapsed to
    /// byte-level Convert.FromHexString/ToHexString instead of legacy's nibble-by-nibble
    /// NbConvert/NbMath helpers — same approach already used by LegacyPasswordVerifier for the
    /// same underlying cipher.</summary>
    public static class HsmDesCipher
    {
        public static string EncryptHex(string dataHex, string keyHex) => Transform(dataHex, keyHex, encrypt: true);

        public static string DecryptHex(string dataHex, string keyHex) => Transform(dataHex, keyHex, encrypt: false);

        private static string Transform(string dataHex, string keyHex, bool encrypt)
        {
            var data = Convert.FromHexString(dataHex);
            var key = Convert.FromHexString(keyHex);

            using SymmetricAlgorithm des = key.Length == 8 ? DES.Create() : TripleDES.Create();
            des.Key = key;
            des.Mode = CipherMode.ECB;
            des.Padding = PaddingMode.None;

            using var transform = encrypt ? des.CreateEncryptor() : des.CreateDecryptor();
            var result = transform.TransformFinalBlock(data, 0, data.Length);

            return Convert.ToHexString(result);
        }

        /// <summary>Byte-wise XOR of two equal-length hex strings — equivalent to legacy
        /// NbMath.BitwiseOperator(..., EnumBitwise.XOR) for the even-length hex strings HSM
        /// components always are (16/32/48 hex chars).</summary>
        public static string XorHex(string firstHex, string secondHex)
        {
            var a = Convert.FromHexString(firstHex);
            var b = Convert.FromHexString(secondHex);
            var result = new byte[a.Length];
            for (var i = 0; i < a.Length; i++)
                result[i] = (byte)(a[i] ^ b[i]);

            return Convert.ToHexString(result);
        }
    }
}
