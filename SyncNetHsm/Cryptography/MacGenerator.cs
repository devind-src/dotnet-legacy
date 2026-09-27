using SyncNet.Helpers;
using System;
using System.Security.Cryptography;

namespace SyncNet.Cryptography
{
    public class MacGenerator
    {
        public enum PaddingMethod
        {
            Method1 = 1,
            Method2 = 2
        }

        public static string CalculateMAC(string dataHex, string keyHex, PaddingMethod paddingMethod)
        {
            string result = "";

            // 16 or 24 bytes for 3DES double length
            if (keyHex.Length != 16 * 2 && keyHex.Length != 24 * 2)
            {
                throw new ArgumentException("Key length must be 16 or 24 bytes (3DES).");
            }

            if (paddingMethod == PaddingMethod.Method1)
                dataHex = ApplyPaddingMethod1(dataHex);
            else
                dataHex = ApplyPaddingMethod2(dataHex);

            if (keyHex.Length == 32)
            {
                string LKey = keyHex.Substring(0, 16);
                string RKey = keyHex.Substring(16, 16);

                string step1 = CalculateMAC(dataHex, LKey); //result Algorithm 1
                string step2 = DecryptCBC(step1, RKey);

                result = EncryptCBC(step2, LKey); //result Algorithm 3
            }
            else if (keyHex.Length == 48)
            {
                string LKey = keyHex.Substring(0, 16);
                string MKey = keyHex.Substring(16, 16);
                string RKey = keyHex.Substring(32, 16);

                string step1 = CalculateMAC(dataHex, LKey); //result Algorithm 1
                string step2 = DecryptCBC(step1, MKey);

                result = EncryptCBC(step2, RKey); //result Algorithm 3
            }

            return result;
        }
        private static string ApplyPaddingMethod1(string inputHex)
        {
            byte[] input = inputHex.HexToBytes();

            return ApplyPaddingMethod1(input).BytesToHex();
        }
        private static string ApplyPaddingMethod2(string inputHex)
        {
            byte[] input = inputHex.HexToBytes();

            return ApplyPaddingMethod2(input).BytesToHex();
        }
        private static byte[] ApplyPaddingMethod1(byte[] inputData)
        {
            int originalLength = inputData.Length;
            int paddedLength = ((originalLength + 8) / 8) * 8;

            byte[] paddedData = new byte[paddedLength];

            Array.Copy(inputData, 0, paddedData, 0, originalLength);

            return paddedData;
        }
        private static byte[] ApplyPaddingMethod2(byte[] inputData)
        {
            int originalLength = inputData.Length;
            int paddedLength = ((originalLength + 8) / 8) * 8;

            byte[] paddedData = new byte[paddedLength];

            Array.Copy(inputData, 0, paddedData, 0, originalLength);

            // Add padding: 0x80 followed by zeros
            paddedData[originalLength] = 0x80;

            return paddedData;
        }
        private static string CalculateMAC(string dataHex, string keyHex)
        {
            // Convert hex strings to byte arrays
            byte[] inputData = dataHex.HexToBytes();
            byte[] keyData = keyHex.HexToBytes();

            using (DES des = DES.Create())
            {
                des.Key = keyData;
                des.Mode = CipherMode.CBC;
                des.Padding = PaddingMode.None; // Padding is applied manually
                des.IV = new byte[8]; // IV is 0x00 for CBC

                // Get the last block from the input data as MAC using 3DES
                byte[] mac = new byte[8];

                using (var encryptor = des.CreateEncryptor())
                {
                    // Split the input data into 8-byte blocks and process each block
                    for (int i = 0; i < inputData.Length; i += 8)
                    {
                        // Skip last block
                        if (i + 8 >= inputData.Length) break;

                        byte[] block = new byte[8];
                        Array.Copy(inputData, i, block, 0, 8);

                        // XOR current block with the previous MAC result
                        for (int j = 0; j < 8; j++)
                        {
                            block[j] ^= mac[j];
                        }

                        // Encrypt the block using DES
                        mac = encryptor.TransformFinalBlock(block, 0, 8);
                    }
                }

                // Return the MAC result in hex
                return mac.BytesToHex();
            }
        }
        private static string EncryptCBC(string dataHex, string keyHex)
        {
            byte[] data = dataHex.HexToBytes();
            byte[] key = keyHex.HexToBytes();

            using (DES des = DES.Create())
            {
                des.Key = key;
                des.Mode = CipherMode.CBC;
                des.Padding = PaddingMode.None;
                des.IV = new byte[8]; //all zeros

                byte[] encrypted = des.CreateEncryptor().TransformFinalBlock(data, 0, data.Length);

                return encrypted.BytesToHex();
            }
        }
        private static string DecryptCBC(string dataHex, string keyHex)
        {
            byte[] data = dataHex.HexToBytes();
            byte[] key = keyHex.HexToBytes();

            using (DES des = DES.Create())
            {
                des.Key = key;
                des.Mode = CipherMode.CBC;
                des.Padding = PaddingMode.None;
                des.IV = new byte[8]; //all zeros

                byte[] decrypted = des.CreateDecryptor().TransformFinalBlock(data, 0, data.Length);

                return decrypted.BytesToHex();
            }
        }
    }
}
