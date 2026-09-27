using SyncNet.Common;
using SyncNet.Helpers;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SyncNet.Library
{
    public class NbLogger
    {
        private string _logdir;
        private string _logname;

        public NbLogger(string LogName)
        {
            //get from parameter
            _logdir = AppConfig.LogDir;
            _logname = LogName;

            if (Directory.Exists(_logdir) == false)
                Directory.CreateDirectory(_logdir);
        }

        public async Task LogAsync(string Message)
        {
            await WriteLogAsync(Message, "", _logname);
        }

        public async Task LogAsync(string Message, string Details)
        {
            await WriteLogAsync(Message, Details, _logname);
        }

        public async Task LogAsync(string Message, string Details, string FileName)
        {
            if (string.IsNullOrEmpty(FileName) == true)
                await WriteLogAsync(Message, Details, _logname);
            else
                await WriteLogAsync(Message, Details, FileName);
        }

        private async Task WriteLogAsync(string Message, string Details, string FileName)
        {
            try
            {
                string dt = DateTime.Now.ToString("yyyy-MM-dd");
                string logname;

                if (AppConfig.IsWindows == true)
                    logname = $@"{_logdir}\{FileName}_{dt}.log";
                else
                    logname = $@"{_logdir}/{FileName.AdjustFileLinux()}_{dt}.log";

                using var fs = new StreamWriter(logname, true);
                await fs.WriteLineAsync(DateTime.Now.ToString("[HH:mm:ss] ") + Message);

                if (Details != "")
                {
                    await fs.WriteLineAsync("{" + Details + "}");
                    await fs.WriteLineAsync();
                }
            }
            catch { }
        }
    }
}
