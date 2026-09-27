using ApiBiller.Common;
using ApiBiller.Helpers;
using SyncNet.Library;
using System;
using System.Data;

namespace ApiBiller.Helpers
{
    static class DataHelper
    {
        public static string AddDecPoint(this decimal amt)
        {
            // format dengan 2 digit desimal
            string val = amt.ToString("0.00");

            // hilangkan tanda titik/comma desimal
            val = val
                .Replace(".", "")
                .Replace(",", "");

            // left pad zeros sampai 12 digit
            return val.PadLeft(12, '0');
        }
        public static string AdjustTranDateTimeGMT(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            if (value.Length != 14) return value;

            DateTime dtTrx = ConvertHelper.ToDateTime(value);
            DateTime dtGmt = dtTrx.AddHours(-7);

            return dtGmt.ToString("MMddHHmmss");
        }
        public static string AdjustTime(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            if (value.Length != 14) return value;

            return value.Substring(8, 6);
        }
        public static string AdjustDate(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            if (value.Length != 14) return value;

            return value.Substring(4, 4);
        }
        public static string AdjustSpace(this string value, int length)
        {
            //if less then 
            string tmp = value.PadRight(length, ' ');

            //if more than 
            return tmp.Substring(0, length);
        }
        public static string AdjustTrace(this string value)
        {
            string tmp = value.PadLeft(12, '0');

            return tmp.Substring(6, 6);
        }
        public static string AdjustOriginalData(this string value)
        {
            string result = "";

            //validate
            if (string.IsNullOrEmpty(value)) return result;
            if (value.Length >= 28)
            {
                //internal: tran type + date time + trace number
                //external: mti + trace + datetime + acq inst id + fwd inst id

                string tran_type = value.Substring(0, 2);
                string date_time = value.Substring(2, 14);
                string trace_number = value.Substring(16, 12);

                result = string.Concat("0200", trace_number.AsSpan(6), date_time.AsSpan(4));
            }

            return result;
        }
        public static string MappingRC(this string value)
        {
            return RespCode.GetRCMapping(value);
        }
        public static string MappingRespMessage(this string value)
        {
            return RespCode.GetRespMessage(value);
        }
    }
}
