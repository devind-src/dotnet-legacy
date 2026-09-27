using System;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;

namespace SyncNet.Services
{
    public static class DukptUtils
    {
        public static BigInteger HexToBigInteger(string hex)
        {
            if (hex.Length % 2 == 1)
                hex = "0" + hex;
            return new BigInteger(Convert.FromHexString(hex.ReverseIfNeeded()), isUnsigned: true, isBigEndian: true);
        }

        public static string BytesToHex(byte[] bytes)
        {
            return Convert.ToHexString(bytes).ToUpperInvariant();
        }

        public static byte[] BigIntegerToBytes(BigInteger value)
        {
            byte[] bytes = value.ToByteArray(isUnsigned: true, isBigEndian: true);
            return bytes.Length % 8 == 0 ? bytes : PadLeft(bytes, GetNearestWholeMultiple(bytes.Length, 8));
        }

        public static BigInteger BytesToBigInteger(byte[] data)
        {
            return new BigInteger(data, isUnsigned: true, isBigEndian: true);
        }

        public static BigInteger TransformDes(BigInteger keyPart, BigInteger message, bool encrypt)
        {
            using SymmetricAlgorithm cipher = DES.Create();
            byte[] key = BigIntegerToBytes(keyPart);
            byte[] paddedKey = PadLeft(key, 8);

            cipher.Key = paddedKey;
            cipher.IV = new byte[8];
            cipher.Mode = CipherMode.CBC;
            cipher.Padding = PaddingMode.Zeros;

            using ICryptoTransform transform = encrypt ? cipher.CreateEncryptor() : cipher.CreateDecryptor();
            byte[] data = BigIntegerToBytes(message);
            byte[] paddedData = PadLeft(data, 8);
            byte[] result = transform.TransformFinalBlock(paddedData, 0, paddedData.Length);

            return BytesToBigInteger(result);
        }

        public static byte[] XorArrays(byte[] a, byte[] b)
        {
            int len = Math.Min(a.Length, b.Length);
            byte[] result = new byte[len];
            for (int i = 0; i < len; i++)
            {
                result[i] = (byte)(a[i] ^ b[i]);
            }
            return result;
        }

        public static byte[] TripleDESEncrypt(byte[] key, byte[] data)
        {
            using var tdes = TripleDES.Create();
            tdes.Key = Adjust3DesKey(key);
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.None;
            using var encryptor = tdes.CreateEncryptor();
            return encryptor.TransformFinalBlock(data, 0, data.Length);
        }

        private static byte[] Adjust3DesKey(byte[] key)
        {
            if (key.Length == 16)
            {
                var fullKey = new byte[24];
                Array.Copy(key, 0, fullKey, 0, 16);
                Array.Copy(key, 0, fullKey, 16, 8);
                return fullKey;
            }
            if (key.Length == 24) return key;
            throw new ArgumentException("Key harus 16 atau 24 byte untuk Triple DES.", nameof(key));
        }

        private static byte[] PadLeft(byte[] data, int length)
        {
            if (data.Length >= length) return data;
            byte[] result = new byte[length];
            Buffer.BlockCopy(data, 0, result, length - data.Length, data.Length);
            return result;
        }

        private static int GetNearestWholeMultiple(int input, int multiple)
        {
            int remainder = input % multiple;
            return remainder == 0 ? input : input + (multiple - remainder);
        }

        private static string ReverseIfNeeded(this string hex)
        {
            return string.Join("", Enumerable.Range(0, hex.Length / 2)
                .Select(i => hex.Substring(i * 2, 2)));
        }
    }
}
