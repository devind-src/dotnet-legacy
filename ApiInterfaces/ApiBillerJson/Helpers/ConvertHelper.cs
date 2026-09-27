using SyncNet.Helpers;
using SyncNet.Library;
using System;

namespace ApiBiller.Helpers
{
    public static class ConvertHelper
    {
        public static DateTime ToDateTime(string value)
        {
            //default value
            DateTime dt = new DateTime(1900, 1, 1, 0, 0, 0);

            if (string.IsNullOrEmpty(value)) return dt;
            if (value.Length != 14) return dt;

            int yyyy = value.Substring(0, 4).ToNumber();
            int mm = value.Substring(4, 2).ToNumber();
            int dd = value.Substring(6, 2).ToNumber();

            int hh = value.Substring(8, 2).ToNumber();
            int mi = value.Substring(10, 2).ToNumber();
            int ss = value.Substring(12, 2).ToNumber();

            return new DateTime(yyyy, mm, dd, hh, mi, ss);
        }
    }
}
