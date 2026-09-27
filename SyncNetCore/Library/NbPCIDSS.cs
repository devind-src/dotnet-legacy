using SyncNet.Cryptography;
using System;
using System.Text;

namespace SyncNet.Library
{
    class NbPCIDSS
    {
        public enum Mode { Encrypt = 0, Decrypt = 1 };

        public static string EncryptMessage(string message, string key)
        {
            if (string.IsNullOrEmpty(message) == true) return "";
            if (string.IsNullOrEmpty(key) == true) return "";

            byte[] bmessage = Encoding.ASCII.GetBytes(message);
            byte[] bchiper = EncryptDecryptMessage(bmessage, key, Mode.Encrypt);

            return Convert.ToBase64String(bchiper);
        }

        public static string DecryptMessage(string message, string key)
        {
            if (string.IsNullOrEmpty(message) == true) return "";
            if (string.IsNullOrEmpty(key) == true) return "";

            byte[] bmessage = Convert.FromBase64String(message);
            byte[] bchiper = EncryptDecryptMessage(bmessage, key, Mode.Decrypt);

            return Encoding.ASCII.GetString(bchiper);
        }

        private static byte[] EncryptDecryptMessage(byte[] message, string key, Mode mode)
        {
            string strHex = NbConvert.BytesToHex(message);
            string strTemp, strEncDec, strResult = "";

            //remove header
            if (mode == Mode.Decrypt)
                strHex = strHex.Substring(2, strHex.Length - 2);

            int sisa = strHex.Length % 16;

            if (sisa > 0)
                strHex = strHex + "".PadLeft(16 - sisa, 'F');

            int maxloop = strHex.Length / 16;

            for (int i = 0; i < maxloop; i++)
            {
                strTemp = strHex.Substring(i * 16, 16);

                if (mode == Mode.Encrypt)
                    strEncDec = DesAlgorithm.EncryptHex(strTemp, key);
                else
                    strEncDec = DesAlgorithm.DecryptHex(strTemp, key);

                strResult = strResult + strEncDec;
            }

            if (mode == Mode.Encrypt) //add header
            {
                if (sisa > 0)
                    strResult = Convert.ToString((16 - sisa) / 2, 16).PadLeft(2, '0') + strResult; //in byte
                else
                    strResult = "00" + strResult;
            }
            else
                strResult = strResult.Substring(0, strResult.Length - (message[0] * 2)); //remove tail

            return NbConvert.HexToBytes(strResult);
        }
    }
}
