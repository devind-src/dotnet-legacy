using System;

namespace SyncNet.Library
{
    public static class NbConvert
    {
        public static string BytesToHex(this byte[] bytes)
        {
            return Convert.ToHexString(bytes);
        }
        public static string BytesToString(this byte[] bytes)
        {
            string ret = "";

            for (int i = 0; i < bytes.Length; i++)
            {
                ret += Convert.ToChar(bytes[i]);
            }

            return ret;
        }
        public static string BytesToString(this byte[] bytes, int index, int length)
        {
            byte[] dest = new byte[length];
            Array.Copy(bytes, index, dest, 0, length);

            return dest.BytesToString();
        }
        public static string StringToHex(string strdata)
        {
            string ret = "";
            string tmp;

            for (int i = 0; i < strdata.Length; i++)
            {
                tmp = strdata.Substring(i, 1);
                tmp = NbString.AscInHex(tmp);

                ret += tmp;
            }

            return ret;
        }
        public static string HexToString(string hexdata)
        {
            string ret = "";

            for (int i = 0; i < hexdata.Length / 2; i++)
            {
                ret += NbString.Chr(NbMath.hex2dec(hexdata.Substring((i * 2) + 1 - 1, 2)));
            }

            return ret;
        }
        public static byte[] HexToBytes(this string hexdata)
        {
            return Convert.FromHexString(hexdata);
        }
        public static byte[] StringToBytes(this string strdata)
        {
            byte[] bytes = new byte[strdata.Length];

            for (int i = 0; i < strdata.Length; i++)
            {
                char tmp = Convert.ToChar(strdata.Substring(i, 1));
                bytes[i] = Convert.ToByte(tmp);
            }

            return bytes;
        }
        public static int ToInt(string value)
        {
            int ret = 0;

            if (int.TryParse(value, out int result) == true)
                ret = result;

            return ret;
        }
    }
}
