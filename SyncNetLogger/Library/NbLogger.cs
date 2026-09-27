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

        public NbLogger()
        {
            //set path log
            _logdir = AppConfig.LogDir;

            if (Directory.Exists(_logdir) == false)
                Directory.CreateDirectory(_logdir);
        }

        public NbLogger(string LogName)
        {
            //set path log
            _logdir = AppConfig.LogDir;
            _logname = LogName.AdjustFileName();

            if (Directory.Exists(_logdir) == false)
                Directory.CreateDirectory(_logdir);
        }

        public async Task LogAsync(string Message)
        {
            //log for internal
            await LogAsync(DateTime.Now, _logname, Message, "");
        }

        public async Task LogAsync(string Message, string Detail)
        {
            //log for internal
            await LogAsync(DateTime.Now, _logname, Message, Detail);
        }

        public async Task LogAsync(DateTime dtNow, string LogName, string Message)
        {
            await LogAsync(dtNow, LogName, Message, "");
        }

        public async Task LogAsync(DateTime dtNow, string LogName, string Message, string Detail)
        {
            try
            {
                string dt = dtNow.ToString("yyyy-MM-dd");
                string logname = $@"{_logdir}/{LogName.AdjustFileName()}_{dt}.log";

                using var fs = new StreamWriter(logname, true);
                await fs.WriteLineAsync(dtNow.ToString("[HH:mm:ss] ") + Message);

                if (Detail != "")
                {
                    await fs.WriteLineAsync("{" + Detail + "}");
                    await fs.WriteLineAsync();
                }
            }
            catch { }
        }
    }
}
