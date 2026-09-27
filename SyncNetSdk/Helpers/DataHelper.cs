using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SyncNet.Helpers
{
    internal static class DataHelper
    {
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

        public static string GetStringFromObject(this object obj)
        {
            string ret = "";

            try
            {
                Dictionary<string, object> dict = obj.ExtractKeyValuePairs();
                if (dict == null) return "";

                foreach (KeyValuePair<string, object> pair in dict)
                {
                    ret += $"{pair.Key}={pair.Value},";
                }

                //remove last comma
                if (ret.Length > 0) ret = ret.Substring(0, ret.Length - 1);
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

        public static string GetSwitchKey(this Message.Request req)
        {
            return req.tran_type.ToUpper() + req.datetime_tran + req.trace_number + req.merchant_id;
        }

        public static string GetSwitchKey(this Message.Response rsp)
        {
            return rsp.tran_type.ToUpper() + rsp.datetime_tran + rsp.trace_number + rsp.merchant_id;
        }

        public static string RemoveNewLine(this string input)
        {
            if (string.IsNullOrEmpty(input)) return "";

            // Hilangkan semua variasi newline
            string noNewLine = input
                .Replace("\r\n", "")
                .Replace("\n", "")
                .Replace("\r", "");

            return noNewLine;
        }

        public static string[] ResizeArray(this string[] arr, int length)
        {
            if (arr == null) return new string[length];
            if (arr.Length == length) return arr;

            var resized = new string[length];
            Array.Copy(arr, resized, Math.Min(arr.Length, length));
            return resized;
        }

        public static Dictionary<string, object> ExtractKeyValuePairs(this object obj)
        {
            // Check if the input object is null
            if (obj == null) return null;

            // Create a dictionary to hold the key-value pairs
            var keyValuePairs = new Dictionary<string, object>();

            try
            {
                // Get the type of the object
                Type type = obj.GetType();

                // Get all properties of the object
                PropertyInfo[] properties = type.GetProperties();

                // Iterate through each property
                foreach (PropertyInfo property in properties)
                {
                    // Get the property name (key)
                    string key = property.Name;

                    // Get the property value
                    object value = property.GetValue(obj);

                    // Add the key-value pair to the dictionary
                    keyValuePairs.Add(key, value);
                }
            }
            catch
            {
                keyValuePairs = null;
            }

            // Return the dictionary
            return keyValuePairs;
        }
    }
}
