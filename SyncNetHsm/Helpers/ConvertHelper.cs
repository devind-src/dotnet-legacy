using System;
using System.Text;

namespace SyncNet.Helpers
{
    public static class ConvertHelper
    {
        public static byte[] HexToBytes(this string data)
        {
            return Convert.FromHexString(data);
        }        
        public static string BytesToHex(this byte[] data)
        {
            return BitConverter.ToString(data).Replace("-", "");
        }
        
        public static int BytesToInt(this byte[] bytes)
        {
            if (bytes.Length != 2) return 0;

            string hex = BytesToHex(bytes);

            return HexToInt(hex);
        }
        public static int HexToInt(this string hex)
        {
            return int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
        }

        public static byte[] StringToBytes(this string strdata)
        {
            return Encoding.UTF8.GetBytes(strdata);
        }
        public static string BytesToString(this byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        public static int ToNumber(this string value)
        {
            int ret = 0;

            if (int.TryParse(value, out int result) == true)
                ret = result;

            return ret;
        }
        public static bool ToBool(this string value)
        {
            bool ret = false;

            if (bool.TryParse(value, out bool result) == true)
                ret = result;

            return ret;
        }
    }
}
