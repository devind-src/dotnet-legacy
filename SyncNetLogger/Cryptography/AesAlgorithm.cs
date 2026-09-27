using SyncNet.Library;
using System;
using System.IO;
using System.Security.Cryptography;

namespace SyncNet.Cryptography
{
    public class AESKeyModel
    {
        public string key { get; set; } = "";
        public string iv { get; set; } = "";
    }
    public class AesAlgorithm
    {
        public enum EnumKeyType
        {
            Hex = 0,
            Base64 = 1
        }
        public enum EnumKeySize
        {
            Size128 = 128,
            Size192 = 192,
            Size256 = 256
        }

        public static AESKeyModel GenerateKey(EnumKeyType type, EnumKeySize keysize)
        {
            AESKeyModel ret = new AESKeyModel();

            using Aes aes = Aes.Create();
            aes.KeySize = (int)keysize;

            if (type == EnumKeyType.Hex)
            {
                ret.key = NbConvert.BytesToHex(aes.Key).ToLower();
                ret.iv = NbConvert.BytesToHex(aes.IV).ToLower();
            }
            else
            {
                ret.key = Convert.ToBase64String(aes.Key);
                ret.iv = Convert.ToBase64String(aes.IV);
            }

            return ret;
        }
        public static string Encrypt(string plain, byte[] key, byte[] IV)
        {
            byte[] encrypted;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = IV;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using MemoryStream msEncrypt = new();
                using CryptoStream csEncrypt = new(msEncrypt, encryptor, CryptoStreamMode.Write);
                using (StreamWriter swEncrypt = new(csEncrypt))
                {
                    swEncrypt.Write(plain);
                }

                encrypted = msEncrypt.ToArray();
            }

            return Convert.ToBase64String(encrypted);
        }
        public static string Decrypt(string encrypted, byte[] key, byte[] IV)
        {
            string plaintext = "";
            byte[] cipher = Convert.FromBase64String(encrypted);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = IV;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using MemoryStream msDecrypt = new(cipher);
                using CryptoStream csDecrypt = new(msDecrypt, decryptor, CryptoStreamMode.Read);
                using StreamReader srDecrypt = new(csDecrypt);

                plaintext = srDecrypt.ReadToEnd();
            }

            return plaintext;
        }
    }
}
