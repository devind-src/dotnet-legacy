using SyncNet.Cryptography;
using System;

namespace SyncNet.Library
{
    public class NbCrypto
    {
        public const byte _OPERAND_XOR = 0;
        public const byte _OPERAND_OR = 1;
        public const byte _OPERAND_AND = 2;

        public enum Mode { Encrypt = 0, Decrypt = 1 };

        public NbCrypto()
        {

        }

        public string GetLicense(string sn, string expdate, string key)
        {
            string actkey = DesAlgorithm.EncryptHex(sn.PadRight(16, 'F'), key);

            expdate = DesAlgorithm.EncryptHex(expdate.PadRight(16, 'F'), key);
            expdate = DesAlgorithm.EncryptHex(expdate, actkey);

            return actkey + expdate;
        }

        public string GetExpDate(string license, string sn, string key)
        {
            if (license.Length != 32)
                return "00000000";

            string actkey = license.Substring(0, 16);
            string expdate = license.Substring(16, 16);

            expdate = DesAlgorithm.DecryptHex(expdate, actkey);
            expdate = DesAlgorithm.DecryptHex(expdate, key);

            return expdate.Substring(0, 8);
        }

        public bool IsLicenseValid(string license, string sn, string key)
        {
            if (license.Length != 32)
                return false;

            string actkey = license.Substring(0, 16);
            string expdate = license.Substring(16, 16);

            expdate = DesAlgorithm.DecryptHex(expdate, actkey);
            expdate = DesAlgorithm.DecryptHex(expdate, key);

            actkey = DesAlgorithm.DecryptHex(actkey, key);

            if (sn == actkey && expdate.Substring(8, 8) == "FFFFFFFF")
                return true;
            else
                return false;
        }

        public string SelectOperand(string FirstValue, string SecondValue, byte Opr)
        {
            string bin_val1;
            string bin_val2;
            string cTemp = "";

            byte a = 0;
            byte b = 0;
            int c = 0;

            bin_val1 = NbMath.hex2bin(FirstValue);
            bin_val2 = NbMath.hex2bin(SecondValue);

            for (int i = 1; i <= bin_val1.Length; i++)
            {
                try
                {
                    a = System.Convert.ToByte(bin_val1.Substring(i - 1, 1));
                    b = System.Convert.ToByte(bin_val2.Substring(i - 1, 1));
                }
                catch { }

                if (Opr == _OPERAND_XOR) //xor
                {
                    c = a ^ b;
                }
                else if (Opr == _OPERAND_OR) //or
                {
                    c = a | b;
                }
                else if (Opr == _OPERAND_AND) //and
                {
                    c = a & b;
                }

                cTemp = cTemp + c;
            }

            return NbMath.bin2hex(cTemp);
        }

        public string EncryptHexString(string dat, string key)
        {
            byte[] msg = NbConvert.HexToBytes(dat);
            byte[] ret = EncryptMessage(msg, key);

            string dst = "";

            try
            {
                dst = NbConvert.BytesToHex(ret);
            }
            catch { }

            return dst;
        }

        public string DecryptHexString(string dat, string key)
        {
            string hex = "";

            try
            {
                int trailer = NbConvert.ToInt(dat.Substring(0, 2));
                byte[] msg = NbConvert.HexToBytes(dat);
                byte[] ret = DecryptMessage(msg, key);

                hex = NbConvert.BytesToHex(ret);
            }
            catch { }

            return hex;
        }

        public byte[] EncryptMessage(byte[] message, string key)
        {
            return EncryptDecryptMessage(message, key, Mode.Encrypt);
        }

        public byte[] DecryptMessage(byte[] message, string key)
        {
            return EncryptDecryptMessage(message, key, Mode.Decrypt);
        }

        private byte[] EncryptDecryptMessage(byte[] message, string key, Mode mode)
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
