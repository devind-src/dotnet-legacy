using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyncNet.Helpers
{
    public static class ConvertHelper
    {
        public static Dictionary<string, string> ArrayToDict(this string[] de_127)
        {
            var dict = new Dictionary<string, string>();

            if (de_127 == null) return dict;

            for (int i = 0; i < de_127.Length; i++)
            {
                if (!string.IsNullOrEmpty(de_127[i]))
                {
                    dict[i.ToString()] = de_127[i];
                }
            }

            return dict;
        }
        public static string[] DictToArray(this Dictionary<string, string> dict)
        {
            if (dict == null || dict.Count == 0)
                return Array.Empty<string>();

            int maxIndex = dict.Keys
                               .Select(k => int.TryParse(k, out int idx) ? idx : -1)
                               .Max();

            var arr = new string[maxIndex + 1];

            foreach (var kvp in dict)
            {
                if (int.TryParse(kvp.Key, out int idx) && idx >= 0)
                {
                    arr[idx] = kvp.Value;
                }
            }

            return arr;
        }

        public static byte[] HexToBytes(this string data)
        {
            return Convert.FromHexString(data);
        }
        public static int HexToInt(this string hex)
        {
            return int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
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
        public static long ToBigNumber(this string value)
        {
            long ret = 0;

            if (long.TryParse(value, out long result) == true)
                ret = result;

            return ret;
        }
        public static decimal ToDecimal(this string value)
        {
            decimal ret = 0;

            if (decimal.TryParse(value, out decimal result) == true)
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
