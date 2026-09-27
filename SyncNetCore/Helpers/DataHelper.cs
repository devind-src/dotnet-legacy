using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SyncNet.Helpers
{
    static class DataHelper
    {
        #region Key Transaction
        public static string GetSwitchKey(this SyncNet.Message.Request req)
        {
            string key = req.tran_type + req.datetime_tran + req.trace_number + req.terminal_id;

            return key.ToUpper();
        }

        public static string GetSwitchKey(this SyncNet.Message.Response rsp)
        {
            string key = rsp.tran_type + rsp.datetime_tran + rsp.trace_number + rsp.terminal_id;

            return key.ToUpper();
        }

        public static string GetSwitchKeyOrig(string original_data, string terminal_id)
        {
            return (original_data + terminal_id).ToUpper();
        }
        #endregion

        public static string AdjustFileName(this string filename)
        {
            string result = filename;

            //validate
            if (string.IsNullOrEmpty(filename)) return result;

            //change to lower case
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == false)
                result = filename.ToLower().Replace(" ", "-");

            return result;
        }

        public static string AdjustFileLinux(this string filename)
        {
            string result = filename;

            //validate
            if (string.IsNullOrEmpty(filename)) return result;

            //change to lower case
            result = filename?.ToLower().Replace(" ", "-");

            return result;
        }


        public static string GetAdditionalData(this Dictionary<string, object> data)
        {
            string ret = string.Empty;

            try
            {
                if (data != null && data.Count > 0)
                {
                    ret = JsonConvert.SerializeObject(data);
                }
            }
            catch { }

            return ret;
        }

        public static string GetMasking(this string data)
        {
            string tmp = data;

            if (string.IsNullOrEmpty(data) == false)
                tmp = "".PadLeft(data.Length, '*');

            return tmp;
        }

        public static string GetOrigData(this string origdata)
        {
            if (string.IsNullOrEmpty(origdata) == true) return "";

            return origdata.ToUpper();
        }


        public static string NormalizeNewLine(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return Environment.NewLine; // kalau kosong, tetap return newline

            // Hilangkan semua variasi newline
            string noNewLine = input
                .Replace("\r\n", "")
                .Replace("\n", "")
                .Replace("\r", "");

            // Tambahkan newline di akhir
            return noNewLine + Environment.NewLine + Environment.NewLine;
        }
    }
}
