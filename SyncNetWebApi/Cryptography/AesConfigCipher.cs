using System.Security.Cryptography;
using System.Text;

namespace SyncNetApi.Cryptography
{
    /// <summary>AES-CBC/PKCS7 cipher backing Tools &gt; Encrypt Credential's default ("JSON")
    /// mode (legacy CredenHelper.EncryptValue), keyed by Resources.bin's AES.Config Key/IV.
    /// Extracted out of ToolsService so Program.cs can decrypt an encrypted connection string
    /// at startup — before the DI container exists — using the same logic.</summary>
    public static class AesConfigCipher
    {
        public static string Encrypt(string plainText, string keyHex, string ivHex)
        {
            using var aes = Aes.Create();
            aes.Key = Convert.FromHexString(keyHex);
            aes.IV = Convert.FromHexString(ivHex);

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            return Convert.ToBase64String(cipherBytes);
        }

        public static string Decrypt(string cipherTextBase64, string keyHex, string ivHex)
        {
            using var aes = Aes.Create();
            aes.Key = Convert.FromHexString(keyHex);
            aes.IV = Convert.FromHexString(ivHex);

            using var decryptor = aes.CreateDecryptor();
            var cipherBytes = Convert.FromBase64String(cipherTextBase64);
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
