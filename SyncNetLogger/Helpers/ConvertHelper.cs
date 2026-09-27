using System.Text;

namespace SyncNet.Helpers
{
    public static class ConvertHelper
    {
        public static byte[] StringToBytes(this string strdata)
        {
            return Encoding.UTF8.GetBytes(strdata);
        }
        public static string BytesToString(this byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        public static bool ToBool(this string val)
        {
            bool.TryParse(val, out bool bval);

            return bval;
        }
        public static int ToNumber(this string val)
        {
            int.TryParse(val, out int ival);

            return ival;
        }
        public static long ToBigNumber(this string val)
        {
            long.TryParse(val, out long lval);

            return lval;
        }
    }
}
