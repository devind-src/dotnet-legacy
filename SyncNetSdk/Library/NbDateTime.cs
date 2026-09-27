using System;
using System.Globalization;

namespace SyncNet.Library
{
    public class NbDateTime
    {
        public static string GetTime()
        {
            return DateTime.Now.ToString("HHmmss");
        }
        public static string GetDate()
        {
            return DateTime.Now.ToString("MMdd");
        }
        public static string GetTransDateTime()
        {
            return DateTime.Now.ToString("MMddHHmmss");
        }

        public static string GetMonthName(int month)
        {
            string ret = "";

            if (month >= 1 && month <= 12)
                ret = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);

            return ret;
        }
        public static string GetMonthName(DateTime dateTime)
        {
            return CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(dateTime.Month);
        }
        public static string GetShortMonthName(int month)
        {
            return CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(month);
        }
        public static string GetShortMonthName(DateTime dateTime)
        {
            return CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(dateTime.Month);
        }
    }
}
