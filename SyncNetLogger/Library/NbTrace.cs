using SyncNet.Common;
using SyncNet.Helpers;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SyncNet.Library
{
    public class NbTrace
    {
        public const byte MessageFrom = 0;
        public const byte MessageTo = 1;

        private NbLogger _logger;
        private bool _isStarted;
        private string _traceDir;

        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks =
            new ConcurrentDictionary<string, SemaphoreSlim>();

        public NbTrace()
        {
            _logger = new NbLogger(MyApp.APPNAME);
            _traceDir = AppConfig.TraceDir;
            _isStarted = true;
        }

        public void TraceOn() => _isStarted = true;
        public void TraceOff() => _isStarted = false;
        public bool IsTraceOn() => _isStarted;

        public async Task WriteTraceStatusAsync(DateTime dtNow, string AppName, string FileTrace, string Info, string Detail = "")
        {
            string dt = dtNow.ToString("[dd MMM yyyy HH:mm:ss.fff] ");
            string content = Detail == ""
                ? dt + Info
                : dt + Info + Environment.NewLine + Detail;

            await WriteLineToFileAsync(AppName, FileTrace, content);
        }
        public async Task WriteTraceMessageAsync(DateTime dtNow, string AppName, string FileTrace, string Title, string Detail)
        {
            string dt = dtNow.ToString("[dd MMM yyyy HH:mm:ss.fff] ");
            string content = dt + Title + Environment.NewLine + Detail + Environment.NewLine;

            await WriteLineToFileAsync(AppName, FileTrace, content);
        }

        private void CheckDirectory(string AppName)
        {
            string dir = _traceDir + "/" + AppName.AdjustFileName() + "/";
            if (Directory.Exists(dir) == false)
                Directory.CreateDirectory(dir);
        }

        private string GetFileName(string AppName, string FileTrace)
        {
            string dt1 = DateTime.Now.ToString("yyyyMMdd_HH");
            return $@"{_traceDir}/{AppName.AdjustFileName()}/{FileTrace.AdjustFileName()}_{dt1}.log";
        }

        private SemaphoreSlim GetLockFor(string filename) =>
            _fileLocks.GetOrAdd(filename, _ => new SemaphoreSlim(1, 1));

        private async Task<bool> WriteLineToFileAsync(string AppName, string FileTrace, string content)
        {
            if (_isStarted == false) return false;

            try
            {
                CheckDirectory(AppName);

                string filename = GetFileName(AppName, FileTrace);
                SemaphoreSlim fileLock = GetLockFor(filename);

                for (int i = 0; i < AppConfig.Settings.RetryPolicy.MaxRetryLog; i++)
                {
                    await fileLock.WaitAsync();
                    try
                    {
                        using var fs = new StreamWriter(filename, true);
                        await fs.WriteLineAsync(content.NormalizeNewlines());
                        return true; // sukses
                    }
                    catch
                    {
                        await Task.Delay(50);
                    }
                    finally
                    {
                        fileLock.Release();
                    }
                }

                await _logger.LogAsync($"NbTrace: gagal menulis trace setelah {AppConfig.Settings.RetryPolicy.MaxRetryLog}x. AppName={AppName}, FileTrace={FileTrace}");
                return false;
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex.Message);
                return false;
            }
        }
    }
}