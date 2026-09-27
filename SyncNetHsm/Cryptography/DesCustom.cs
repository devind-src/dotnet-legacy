using System;
using System.Security.Cryptography;
using System.Text;

namespace SyncNet.Cryptography
{
    public class DesCustom
    {
        #region Trple DES ECB
        public static string SingleDESEncryptECB(string plaintextHex, string key)
        {
            byte[] keyBytes = HexStringToByteArray(key);
            byte[] plaintextBytes = HexStringToByteArray(plaintextHex);

            using (DES des = DES.Create())
            {
                des.Key = keyBytes;
                des.Mode = CipherMode.ECB;
                des.Padding = PaddingMode.None;

                using (ICryptoTransform encryptor = des.CreateEncryptor())
                {
                    byte[] encryptedBytes = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

                    return ByteArrayToHexString(encryptedBytes);
                }
            }
        }

        public static string SingleDESDecryptECB(string encryptedTextHex, string key)
        {
            byte[] keyBytes = HexStringToByteArray(key);
            byte[] encryptedBytes = HexStringToByteArray(encryptedTextHex);

            using (DES des = DES.Create())
            {
                des.Key = keyBytes;
                des.Mode = CipherMode.ECB;
                des.Padding = PaddingMode.None;

                using (ICryptoTransform decryptor = des.CreateDecryptor())
                {
                    byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

                    return ByteArrayToHexString(decryptedBytes);
                }
            }
        }

        public static string TripleDESEncryptECB(string plaintextHex, string key)
        {
            key = AdjustKeyLength(key);

            byte[] keyBytes = HexStringToByteArray(key);
            byte[] plaintextBytes = HexStringToByteArray(plaintextHex);

            byte[] encryptedBytes = SingleDESEncryptECB(plaintextBytes, keyBytes, 0); // Enkripsi pertama dengan kunci pertama
            encryptedBytes = SingleDESDecryptECB(encryptedBytes, keyBytes, 1); // Dekrip kedua dengan kunci kedua
            encryptedBytes = SingleDESEncryptECB(encryptedBytes, keyBytes, 2); // Enkripsi ketiga dengan kunci ketiga

            return ByteArrayToHexString(encryptedBytes);
        }

        public static string TripleDESDecryptECB(string encryptedTextHex, string key)
        {
            key = AdjustKeyLength(key);

            byte[] keyBytes = HexStringToByteArray(key);
            byte[] encryptedBytes = HexStringToByteArray(encryptedTextHex);

            byte[] decryptedBytes = SingleDESDecryptECB(encryptedBytes, keyBytes, 2); // Dekripsi pertama dengan kunci ketiga
            decryptedBytes = SingleDESEncryptECB(decryptedBytes, keyBytes, 1); // Enkript kedua dengan kunci kedua
            decryptedBytes = SingleDESDecryptECB(decryptedBytes, keyBytes, 0); // Dekripsi ketiga dengan kunci pertama

            return ByteArrayToHexString(decryptedBytes);
        }

        private static byte[] SingleDESEncryptECB(byte[] inputBytes, byte[] keyBytes, int keyIndex)
        {
            byte[] key = new byte[8];
            Array.Copy(keyBytes, keyIndex * 8, key, 0, 8);

            using (DES des = DES.Create())
            {
                des.Key = key;
                des.Mode = CipherMode.ECB;
                des.Padding = PaddingMode.None;

                using (ICryptoTransform encryptor = des.CreateEncryptor())
                {
                    return encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
                }
            }
        }

        private static byte[] SingleDESDecryptECB(byte[] inputBytes, byte[] keyBytes, int keyIndex)
        {
            byte[] key = new byte[8];
            Array.Copy(keyBytes, keyIndex * 8, key, 0, 8);

            using (DES des = DES.Create())
            {
                des.Key = key;
                des.Mode = CipherMode.ECB;
                des.Padding = PaddingMode.None;

                using (ICryptoTransform decryptor = des.CreateDecryptor())
                {
                    return decryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
                }
            }
        }
        #endregion

   
        #region Trple DES CBC
        public static string SingleDESEncryptCBC(string plaintextHex, string key, string iv)
        {
            byte[] keyBytes = HexStringToByteArray(key);
            byte[] ivBytes = HexStringToByteArray(iv);
            byte[] plaintextBytes = HexStringToByteArray(plaintextHex);

            using (DES des = DES.Create())
            {
                des.Key = keyBytes;
                des.Mode = CipherMode.CBC;
                des.IV = ivBytes;
                des.Padding = PaddingMode.None;

                using (ICryptoTransform encryptor = des.CreateEncryptor())
                {
                    byte[] encryptedBytes = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

                    return ByteArrayToHexString(encryptedBytes);
                }
            }
        }

        public static string SingleDESDecryptCBC(string encryptedTextHex, string key, string iv)
        {
            byte[] keyBytes = HexStringToByteArray(key);
            byte[] ivBytes = HexStringToByteArray(iv);
            byte[] encryptedBytes = HexStringToByteArray(encryptedTextHex);

            using (DES des = DES.Create())
            {
                des.Key = keyBytes;
                des.Mode = CipherMode.CBC;
                des.IV = ivBytes;
                des.Padding = PaddingMode.None;

                using (ICryptoTransform decryptor = des.CreateDecryptor())
                {
                    byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

                    return ByteArrayToHexString(decryptedBytes);
                }
            }
        }

        public static string TripleDESEncryptCBC(string plaintextHex, string key, string iv)
        {
            key = AdjustKeyLength(key);

            byte[] keyBytes = HexStringToByteArray(key);
            byte[] ivBytes = HexStringToByteArray(iv);
            byte[] plaintextBytes = HexStringToByteArray(plaintextHex);

            byte[] encryptedBytes = SingleDESEncryptCBC(plaintextBytes, keyBytes, ivBytes, 0); // Enkripsi pertama dengan kunci pertama
            encryptedBytes = SingleDESDecryptCBC(encryptedBytes, keyBytes, ivBytes, 1); // Dekrip kedua dengan kunci kedua
            encryptedBytes = SingleDESEncryptCBC(encryptedBytes, keyBytes, ivBytes, 2); // Enkripsi ketiga dengan kunci ketiga

            return ByteArrayToHexString(encryptedBytes);
        }

        public static string TripleDESDecryptCBC(string encryptedTextHex, string key, string iv)
        {
            key = AdjustKeyLength(key);

            byte[] keyBytes = HexStringToByteArray(key);
            byte[] ivBytes = HexStringToByteArray(iv);
            byte[] encryptedBytes = HexStringToByteArray(encryptedTextHex);

            byte[] decryptedBytes = SingleDESDecryptCBC(encryptedBytes, keyBytes, ivBytes, 2); // Dekripsi pertama dengan kunci ketiga
            decryptedBytes = SingleDESEncryptCBC(decryptedBytes, keyBytes, ivBytes, 1); // Enkript kedua dengan kunci kedua
            decryptedBytes = SingleDESDecryptCBC(decryptedBytes, keyBytes, ivBytes, 0); // Dekripsi ketiga dengan kunci pertama

            return ByteArrayToHexString(decryptedBytes);
        }

        private static byte[] SingleDESEncryptCBC(byte[] inputBytes, byte[] keyBytes, byte[] ivBytes, int keyIndex)
        {
            byte[] key = new byte[8];
            Array.Copy(keyBytes, keyIndex * 8, key, 0, 8);

            using (DES des = DES.Create())
            {
                des.Key = key;
                des.Mode = CipherMode.CBC;
                des.IV = ivBytes;
                des.Padding = PaddingMode.None;

                using (ICryptoTransform encryptor = des.CreateEncryptor())
                {
                    return encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
                }
            }
        }

        private static byte[] SingleDESDecryptCBC(byte[] inputBytes, byte[] keyBytes, byte[] ivBytes, int keyIndex)
        {
            byte[] key = new byte[8];
            Array.Copy(keyBytes, keyIndex * 8, key, 0, 8);

            using (DES des = DES.Create())
            {
                des.Key = key;
                des.Mode = CipherMode.CBC;
                des.IV = ivBytes;
                des.Padding = PaddingMode.None;

                using (ICryptoTransform decryptor = des.CreateDecryptor())
                {
                    return decryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
                }
            }
        }
        #endregion

      
        #region Helper
        private static string AdjustKeyLength(string key)
        {
            string result;

            switch (key.Length)
            {
                case 16:
                    result = key + key + key;
                    break;
                case 32:
                    string k1 = key.Substring(0, 16);
                    string k2 = key.Substring(16, 16);
                    string k3 = k1;

                    result = k1 + k2 + k3;

                    break;
                default:
                    result = key;
                    break;
            }

            return result;
        }

        private static byte[] HexStringToByteArray(string hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        private static string ByteArrayToHexString(byte[] byteArray)
        {
            StringBuilder hex = new StringBuilder(byteArray.Length * 2);
            foreach (byte b in byteArray)
            {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString().ToUpper();
        }
        #endregion
    }
}
