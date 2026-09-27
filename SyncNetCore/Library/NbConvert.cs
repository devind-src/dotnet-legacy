using System;
using System.Globalization;
using System.Text;

namespace SyncNet.Library
{
    public class NbConvert
    {
        // Number Conversion
        public static int ToInt(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return 0;

            // coba parse sebagai int langsung
            if (int.TryParse(str, out var val)) return val;

            // kalau gagal, coba parse sebagai decimal lalu cast ke int
            return (int)ToDecimal(str);
        }
        public static long ToLong(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return 0;

            // coba parse sebagai long langsung
            if (long.TryParse(str, out var val)) return val;

            // kalau gagal, coba parse sebagai decimal lalu cast ke long
            return (long)ToDecimal(str);
        }
        public static double ToDouble(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return 0;

            // coba parse dengan InvariantCulture (US style: 123,456.00)
            if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                return val;

            // coba parse dengan Indonesia culture (ID style: 123.456,00)
            if (double.TryParse(str, NumberStyles.Any, new CultureInfo("id-ID"), out val))
                return val;

            return 0;
        }
        public static decimal ToDecimal(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return 0;

            // coba parse dengan InvariantCulture (US style: 123,456.00)
            if (decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                return val;

            // coba parse dengan Indonesia culture (ID style: 123.456,00)
            if (decimal.TryParse(str, NumberStyles.Any, new CultureInfo("id-ID"), out val))
                return val;

            return 0;
        }

        public static int HexToInt(string hex) =>
            int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var val) ? val : -1;
        public static string IntToHex(int num) => num.ToString("X");


        // Base64
        public static string BytesToBase64(byte[] bytes) => Convert.ToBase64String(bytes);
        public static string EncodeBase64(string val) => Convert.ToBase64String(Encoding.UTF8.GetBytes(val));
        public static string DecodeBase64(string val) => Encoding.UTF8.GetString(Convert.FromBase64String(val));


        // String Hex Bytes
        public static byte[] HexToBytes(string hex) => Convert.FromHexString(hex);
        public static string HexToString(string hex)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < hex.Length; i += 2)
                sb.Append((char)Convert.ToInt32(hex.Substring(i, 2), 16));
            return sb.ToString();
        }

        public static byte[] StringToBytes(string strdata)
        {
            //kendala utk character tertentu, convert balik char berubah
            //return Encoding.UTF8.GetBytes(strdata);

            byte[] bytes = new byte[strdata.Length];

            for (int i = 0; i < strdata.Length; i++)
            {
                bytes[i] = NbString.Asc(strdata.Substring(i, 1));
            }

            return bytes;
        }
        public static string StringToHex(string str)
        {
            var sb = new StringBuilder();
            foreach (var c in str)
                sb.Append(((int)c).ToString("X2"));
            return sb.ToString();
        }

        public static string BytesToHex(byte[] bytes) => Convert.ToHexString(bytes);
        public static string BytesToString(byte[] bytes)
        {
            //kendala utk character tertentu, convert balik char berubah
            //return Encoding.UTF8.GetString(bytes);

            string ret = "";

            for (int i = 0; i < bytes.Length; i++)
            {
                ret = ret + NbString.Chr(bytes[i]);
            }

            return ret;
        }
        public static string BytesToString(byte[] bytes, int index, int length)
        {
            byte[] dest = new byte[length];
            Array.Copy(bytes, index, dest, 0, length);

            return BytesToString(dest);
        }

        public static string ArrayToString(string[] arr) => string.Concat(arr);


        // ASCII EBCDIC
        public static byte[] AsciiToEbcdic(byte[] asciiData) =>
            Encoding.Convert(Encoding.ASCII, Encoding.GetEncoding("IBM037"), asciiData);
        public static byte[] EbcdicToAscii(byte[] ebcdicData) =>
            Encoding.Convert(Encoding.GetEncoding("IBM037"), Encoding.ASCII, ebcdicData);
        public static string AsciiToEbcdic(string asciiData) =>
            BytesToString(AsciiToEbcdic(StringToBytes(asciiData)));
        public static string EbcdicToAscii(string ebcdicData) =>
            BytesToString(EbcdicToAscii(StringToBytes(ebcdicData)));


        // BCD
        public static string ToBCD(string str, bool padRight = false)
        {
            if (str.Length % 2 != 0)
                str = padRight ? str + "0" : "0" + str;

            var sb = new StringBuilder();
            for (int i = 0; i < str.Length; i += 2)
            {
                var hex = str.Substring(i, 2);
                sb.Append((char)Convert.ToInt32(hex, 16));
            }
            return sb.ToString();
        }
        public static string FromBCD(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            var sb = new StringBuilder();
            foreach (var b in bytes)
                sb.Append(b.ToString("X2"));
            return sb.ToString();
        }


        // DateTime
        public static DateTime ToDateTime(string str) =>
            DateTime.TryParse(str, out var dt) ? dt : default;
        public static DateTime ToDateTime(object obj) =>
            DateTime.TryParse(obj.ToString(), out var dt) ? dt : default;
    }
}
