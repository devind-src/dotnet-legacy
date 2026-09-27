using System;
using System.Runtime.InteropServices;

namespace SyncNet.Helpers
{
    static class SystemHelper
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
        public static string NormalizeNewlines(this string input)
        {
            //validate
            if (string.IsNullOrEmpty(input)) return "";

            //validate one line
            if (!input.Contains("\n") && !input.Contains("\r")) return input;

            //remove all newline
            string trimmed = input.TrimEnd('\r', '\n');

            //add newline
            return trimmed + Environment.NewLine;
        }
    }
}
