using Newtonsoft.Json;
using SyncNet.Library;
using SyncNet.Models;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SyncNet.Helpers
{
    public static class DataHelper
    {
        public static string AdjustFileName(this string filename)
        {
            string result = filename;

            //validate
            if (string.IsNullOrEmpty(filename)) return result;

            //change to lower case
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == false)
                result = result?.ToLower().Replace(" ", "-");

            return result;
        }
        public static byte[] AddTcpHeader(this string strdata)
        {
            byte[] data = NbConvert.StringToBytes(strdata);

            return AddTcpHeader(data);
        }
        public static byte[] AddTcpHeader(this byte[] data)
        {
            int length = data.Length;
            string hex = length.ToString("X").PadLeft(4, '0');
            byte[] header = hex.HexToBytes();

            // Create the final byte array with header and data
            byte[] result = new byte[header.Length + data.Length];
            Array.Copy(header, 0, result, 0, header.Length);
            Array.Copy(data, 0, result, header.Length, data.Length);

            return result;
        }

        public static string PadLeftZero(this string val, int num)
        {
            if (string.IsNullOrEmpty(val)) return "".PadLeft(num, '0');

            return val.PadLeft(num, '0');
        }
        public static string FormatBinary(this byte[] bytes)
        {
            string strdata = NbConvert.BytesToString(bytes);

            const short LENGTH_ROW = 16;

            int lengthRow = strdata.Length;
            int maxLoop = lengthRow / LENGTH_ROW;

            string ret = "";
            string tmp = "";

            string sbin;
            string shex;

            for (int i = 0; i <= maxLoop; i++)
            {
                if (i == maxLoop)
                    sbin = strdata.Substring(i * LENGTH_ROW, lengthRow - (i * LENGTH_ROW));
                else
                    sbin = strdata.Substring(i * LENGTH_ROW, LENGTH_ROW);

                shex = "";

                for (int j = 0; j < sbin.Length; j++)
                {
                    tmp = sbin.Substring(j, 1);
                    tmp = NbString.AscInHex(tmp);

                    shex = shex + " " + tmp;
                }

                //display binary + hex
                if (sbin.Length < 16)
                    sbin = sbin + "".PadLeft(16 - sbin.Length, ' ');

                ret = ret + "[" + (i * LENGTH_ROW).ToString().PadLeft(5, '0') + "]  " + FormatString(sbin) + "  " + shex + "\r\n";
            }

            return ret;
        }
        public static string FormatString(this string strdata)
        {
            string ret = "";

            for (int i = 0; i < strdata.Length; i++)
            {
                if (NbString.Asc(strdata.Substring(i, 1)) < 32)
                    ret = ret + ".";
                else if (NbString.Asc(strdata.Substring(i, 1)) > 126)
                    ret = ret + ".";
                else
                    ret = ret + strdata.Substring(i, 1);
            }

            return ret;
        }

        public static async Task<string> ReadAsStringAsync(this Stream requestBody, bool leaveOpen = false)
        {
            using StreamReader reader = new(requestBody, leaveOpen: leaveOpen);
            var bodyAsString = await reader.ReadToEndAsync();

            return bodyAsString;
        }

        public static BaseResponse GetBaseResponseObj(string msg)
        {
            string[] arr = msg.Split(':');

            BaseResponse b = new()
            {
                resp_code = arr[0],
                resp_message = arr[1]
            };

            return b;
        }
        public static BaseResponse GetBaseResponseObj(string rc, string msg)
        {
            BaseResponse b = new()
            {
                resp_code = rc,
                resp_message = msg
            };

            return b;
        }

        public static string GetBaseResponseString(string msg)
        {
            string[] arr = msg.Split(':');

            BaseResponse b = new()
            {
                resp_code = arr[0],
                resp_message = arr[1]
            };

            return JsonConvert.SerializeObject(b);
        }
        public static string GetBaseResponseString(string rc, string msg)
        {
            BaseResponse b = new()
            {
                resp_code = rc,
                resp_message = msg
            };

            return JsonConvert.SerializeObject(b);
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
