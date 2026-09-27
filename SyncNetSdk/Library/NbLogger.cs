using Newtonsoft.Json;
using SyncNet.Common;
using SyncNet.Helpers;
using System;
using System.IO;
using System.Runtime.InteropServices;
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
            _logdir = SdkConfig.LogDir;
            _logname = LogName;

            if (Directory.Exists(_logdir) == false)
                Directory.CreateDirectory(_logdir);
        }

        
        #region LogSync
        public void Log(string Message)
        {
            WriteLog(Message, "", _logname);
        }

        public void Log(string Message, string Details)
        {
            WriteLog(Message, Details, _logname);
        }

        public void Log(string Message, string Details, string FileName)
        {
            if (string.IsNullOrEmpty(FileName) == true)
                WriteLog(Message, Details, _logname);
            else
                WriteLog(Message, Details, FileName);
        }

        public void Log(Exception ex, string sql, object param = null)
        {
            var logEntry = new
            {
                Sql = sql,
                Params = param,
                Error = ex.Message.RemoveNewLine(),
            };

            Log($"[DB ERROR] {JsonConvert.SerializeObject(logEntry)}");
        }

        private void WriteLog(string Message, string Details, string FileName)
        {
            for (int i = 0; i < SdkConfig.Settings.RetryPolicy.MaxRetryLog; i++)
            {
                try
                {
                    string dt = DateTime.Now.ToString("yyyy-MM-dd");
                    string logname;

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        logname = $@"{_logdir}\{FileName}_{dt}.log";
                    else
                        logname = $@"{_logdir}/{FileName.AdjustFileLinux()}_{dt}.log";

                    using var fs = new StreamWriter(logname, true);
                    fs.WriteLine(DateTime.Now.ToString("[HH:mm:ss] ") + Message);

                    if (Details != "")
                    {
                        fs.WriteLine("{" + Details + "}");
                        fs.WriteLine();
                    }

                    //exit when success
                    break;
                }
                catch
                {
                    //try until success
                }
            }
        }
        #endregion


        #region LogAsync
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

        public async Task LogAsync(Exception ex, string sql, object param = null)
        {
            var logEntry = new
            {
                Sql = sql,
                Params = param,
                Error = ex.Message.RemoveNewLine(),
            };

            await LogAsync($"[DB ERROR] {JsonConvert.SerializeObject(logEntry)}");
        }

        private async Task WriteLogAsync(string Message, string Details, string FileName)
        {
            try
            {
                string dt = DateTime.Now.ToString("yyyy-MM-dd");
                string logname;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    logname = $@"{_logdir}\{FileName}_{dt}.log";
                else
                    logname = $@"{_logdir}/{FileName.AdjustFileLinux()}_{dt}.log";

                await using var fs = new StreamWriter(logname, true);
                await fs.WriteLineAsync(DateTime.Now.ToString("[HH:mm:ss] ") + Message);

                if (Details != "")
                {
                    await fs.WriteLineAsync("{" + Details + "}");
                    await fs.WriteLineAsync();
                }
            }
            catch { }
        }
        #endregion
    }
}
