using SyncNet.Library;
using System;
using System.Security.Cryptography;

namespace SyncNet.Cryptography
{
    public class DesAlgorithm
    {
        private enum EnumMode
        {
            Encrypt = 0,
            Decrypt = 1,
        }

        public static string EncryptHex(string hex, string key)
        {
            string ret = "".PadLeft(16, '0');

            switch (key.Length)
            {
                case 16:
                    ret = DesEncrypt(hex, key);
                    break;
                case 32:
                case 48:
                    ret = TripleDesEncrypt(hex, key);
                    break;
            }

            return ret;
        }

        public static string DecryptHex(string hex, string key)
        {
            string ret = "".PadLeft(16, '0');

            switch (key.Length)
            {
                case 16:
                    ret = DesDecrypt(hex, key);
                    break;
                case 32:
                case 48:
                    ret = TripleDesDecrypt(hex, key);
                    break;
            }

            return ret;
        }


        #region Triple DES
        private static string TripleDesEncrypt(string hex, string key)
        {
            byte[] p = NbConvert.HexToBytes(hex);
            byte[] k = NbConvert.HexToBytes(key);

            return NbConvert.BytesToHex(TripleDesEncrypt(p, k));
        }

        private static string TripleDesDecrypt(string hex, string key)
        {
            byte[] p = NbConvert.HexToBytes(hex);
            byte[] k = NbConvert.HexToBytes(key);

            return NbConvert.BytesToHex(TripleDesDecrypt(p, k));
        }

        private static byte[] TripleDesEncrypt(byte[] hex, byte[] key)
        {
            byte[] encrypted = [];

            try
            {
                // Create a new 3DES key.
                using var des = TripleDES.Create();

                // Set the KeySize = 192 for 168-bit DES encryption.
                // The msb of each byte is a parity bit, so the key length is actually 168 bits.
                des.KeySize = 192;
                des.Key = key;
                des.Mode = CipherMode.ECB;
                des.Padding = PaddingMode.None;

                using var encryptor = des.CreateEncryptor();
                encrypted = encryptor.TransformFinalBlock(hex, 0, hex.Length);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }

            return encrypted;
        }

        private static byte[] TripleDesDecrypt(byte[] hex, byte[] key)
        {
            byte[] decrypted = [];

            try
            {
                // Create a new 3DES key.
                using var des = TripleDES.Create();

                // Set the KeySize = 192 for 168-bit DES encryption.
                // The msb of each byte is a parity bit, so the key length is actually 168 bits.
                des.KeySize = 192;
                des.Key = key;
                des.Mode = CipherMode.ECB;
                des.Padding = PaddingMode.None;

                using var encryptor = des.CreateDecryptor();
                decrypted = encryptor.TransformFinalBlock(hex, 0, hex.Length);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }

            return decrypted;
        }
        #endregion


        #region Single DES
        private static string DesEncrypt(string hex, string key)
        {
            byte[] p = NbConvert.HexToBytes(hex);
            byte[] k = NbConvert.HexToBytes(key);

            return NbConvert.BytesToHex(DesEncrypt(p, k));
        }

        private static string DesDecrypt(string hex, string key)
        {
            byte[] p = NbConvert.HexToBytes(hex);
            byte[] k = NbConvert.HexToBytes(key);

            return NbConvert.BytesToHex(DesDecrypt(p, k));
        }

        private static byte[] DesEncrypt(byte[] hex, byte[] key)
        {
            byte[] encrypted = [];

            try
            {
                // Create a new DES key.
                using var des = DES.Create();

                // Set the KeySize = 64 for 56-bit DES encryption.
                // The msb of each byte is a parity bit, so the key length is actually 168 bits.
                des.KeySize = 64;
                des.Key = key;
                des.Mode = CipherMode.ECB;
                des.Padding = PaddingMode.None;

                using var encryptor = des.CreateEncryptor();
                encrypted = encryptor.TransformFinalBlock(hex, 0, hex.Length);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }

            return encrypted;
        }

        private static byte[] DesDecrypt(byte[] hex, byte[] key)
        {
            byte[] decrypted = [];

            try
            {
                // Create a new DES key.
                using var des = DES.Create();

                // Set the KeySize = 64 for 56-bit DES encryption.
                // The msb of each byte is a parity bit, so the key length is actually 168 bits.
                des.KeySize = 64;
                des.Key = key;
                des.Mode = CipherMode.ECB;
                des.Padding = PaddingMode.None;

                using var encryptor = des.CreateDecryptor();
                decrypted = encryptor.TransformFinalBlock(hex, 0, hex.Length);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }

            return decrypted;
        }
        #endregion


        #region Encryption Triple DES for internal only
        public static string EncryptText(string plain_text, string hex_key)
        {
            byte[] bytes = NbConvert.StringToBytes(plain_text);

            byte[] encrypted = EncryptDecryptMessage(bytes, hex_key, EnumMode.Encrypt);

            return NbConvert.BytesToHex(encrypted);
        }

        public static string DecryptText(string encrypted_text, string hex_key)
        {
            byte[] bytes = NbConvert.HexToBytes(encrypted_text);

            byte[] plain = EncryptDecryptMessage(bytes, hex_key, EnumMode.Decrypt);

            return NbConvert.BytesToString(plain);
        }

        private static byte[] EncryptDecryptMessage(byte[] message, string hex_key, EnumMode mode)
        {
            string strHex = NbConvert.BytesToHex(message);
            string strTemp, strEncDec, strResult = "";

            //remove header
            if (mode == EnumMode.Decrypt)
                strHex = strHex.Substring(2, strHex.Length - 2);

            int sisa = strHex.Length % 16;

            if (sisa > 0)
                strHex = strHex + "".PadLeft(16 - sisa, 'F');

            int maxloop = strHex.Length / 16;

            for (int i = 0; i < maxloop; i++)
            {
                strTemp = strHex.Substring(i * 16, 16);

                if (mode == EnumMode.Encrypt)
                    strEncDec = DesAlgorithm.EncryptHex(strTemp, hex_key);
                else
                    strEncDec = DesAlgorithm.DecryptHex(strTemp, hex_key);

                strResult = strResult + strEncDec;
            }

            if (mode == EnumMode.Encrypt) //add header
            {
                if (sisa > 0)
                    strResult = Convert.ToString((16 - sisa) / 2, 16).PadLeft(2, '0') + strResult; //in byte
                else
                    strResult = "00" + strResult;
            }
            else
            {
                int num_tail = message[0] * 2;

                strResult = strResult.Substring(0, strResult.Length - num_tail); //remove tail
            }

            return NbConvert.HexToBytes(strResult);
        }
        #endregion
    }
}
