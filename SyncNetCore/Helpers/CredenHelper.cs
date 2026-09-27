using SyncNet.Common;
using SyncNet.Cryptography;
using System;
using System.IO;
using System.Security.Cryptography;

namespace SyncNet.Helpers
{
    public static class CredenHelper
    {
        private static byte[] Key;
        private static byte[] IV;

        public static void Initialize()
        {
            Resources.Initialize();

            Key = Convert.FromHexString(Resources.Keys.AES.Config.Key);
            IV = Convert.FromHexString(Resources.Keys.AES.Config.IV);
        }
        public static string EncryptValue(this string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                using Aes aes = Aes.Create();
                aes.Key = Key;
                aes.IV = IV;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using var ms = new MemoryStream();
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                }

                return Convert.ToBase64String(ms.ToArray());
            }
            catch { }

            return string.Empty;
        }
        public static string DecryptValue(this string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                using Aes aes = Aes.Create();
                aes.Key = Key;
                aes.IV = IV;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);

                return sr.ReadToEnd();
            }
            catch { }

            return string.Empty;
        }

        public static string DecryptConnString(this string encrypted)
        {
            return AesAlgorithm.Decrypt(encrypted, Key, IV);
        }
        public static string EncryptConnString(this string encrypted)
        {
            return AesAlgorithm.Encrypt(encrypted, Key, IV);
        }
    }
}
