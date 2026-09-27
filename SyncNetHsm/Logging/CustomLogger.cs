using SyncNet.Common;
using SyncNet.Helpers;
using System;
using System.IO;

namespace SyncNet.Logging
{
    public class CustomLogger
    {
        public void Log(string info)
        {
            Log(info, "");
        }

        public void Log(string info, string detail)
        {
            try
            {
                string date = DateTime.Now.ToString("yyyyMMdd");
                string path = $@"{AppConfig.LogDir}";
                string filename = Path.Combine(path, $"{AppConfig.APPNAME.AdjustFileName()}_{date}.log");

                //make sure directory exist
                if (Directory.Exists(path) == false)
                    Directory.CreateDirectory(path);

                //write log
                using StreamWriter fs = new StreamWriter(filename, true);
                if (string.IsNullOrEmpty(detail) == true)
                {
                    fs.WriteAsync($"[{DateTime.Now.ToString("HH:mm:ss.fff")}] ");
                    fs.WriteLineAsync(info);
                }
                else
                {
                    fs.WriteAsync($"[{DateTime.Now.ToString("HH:mm:ss.fff")}] ");
                    fs.WriteLineAsync(info);
                    fs.WriteLineAsync(detail);
                    fs.WriteLineAsync("");
                }

                //close
                fs.Close();
            }
            catch { }
        }
    }
}
