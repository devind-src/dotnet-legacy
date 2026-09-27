using Newtonsoft.Json;
using SyncNet.Common;
using SyncNet.Helpers;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SyncNet.Library
{
    /// <summary>
    /// Menulis trace/log untuk request-response transaksi, dengan masking data sensitif (PAN, track2).
    /// </summary>
    public sealed class NbTrace
    {
        public const byte MessageFrom = 0;
        public const byte MessageTo = 1;

        private readonly SemaphoreSlim _fileLock = new(1, 1);
        private readonly string _traceDir;
        private readonly string _appName;
        private volatile bool _isStarted = true;

        public NbTrace(string appName)
        {
            _appName = appName;
            _traceDir = SdkConfig.TraceDir;

            Directory.CreateDirectory(GetTraceDirectory());
        }

        public bool IsTraceOn => _isStarted;

        public void TraceOn()
        {
            _isStarted = true;
            WriteTraceStatus("Trace Started");
        }

        public void TraceOff()
        {
            WriteTraceStatus("Trace Stopped");
            _isStarted = false;
        }

        public string GetMaskingRequest(Message.Request req)
        {
            // deep clone supaya req asli tidak berubah
            var clone = JsonConvert.DeserializeObject<Message.Request>(JsonConvert.SerializeObject(req));

            clone.pan = clone.pan.GetMasking();
            clone.security.track2data = clone.security.track2data.GetMasking();

            return JsonConvert.SerializeObject(clone);
        }

        public string GetMaskingResponse(Message.Response rsp)
        {
            // deep clone supaya req asli tidak berubah
            var clone = JsonConvert.DeserializeObject<Message.Response>(JsonConvert.SerializeObject(rsp));

            clone.pan = clone.pan.GetMasking();
            clone.security.track2data = clone.security.track2data.GetMasking();

            return JsonConvert.SerializeObject(clone);
        }

        public void WriteTraceStatus(string message, string details = "")
        {
            if (!_isStarted) return;

            string filename = GetFileName(_appName);
            WriteLineWithRetry(filename, FormatLine(message, details));
        }

        public void WriteTraceMessage(string msgType, string streamMessage, byte status, string nodeName, string remoteIp)
        {
            if (!_isStarted) return;

            string direction = status == MessageFrom ? "from" : "to";
            string title = $"<{msgType}> Message {direction} {nodeName} {remoteIp}"; // e.g. <0200> message from TM
            string filename = GetFileName(nodeName);

            WriteLineWithRetry(filename, FormatLine(title, streamMessage) + Environment.NewLine);
        }

        public Task WriteTraceStatusAsync(string message, string details = "", CancellationToken ct = default)
        {
            if (!_isStarted) return Task.CompletedTask;

            string filename = GetFileName(_appName);
            return WriteLineWithRetryAsync(filename, FormatLine(message, details), ct);
        }

        public Task WriteTraceMessageAsync(string msgType, string streamMessage, byte status, string nodeName, string remoteIp, CancellationToken ct = default)
        {
            if (!_isStarted) return Task.CompletedTask;

            string direction = status == MessageFrom ? "from" : "to";
            string title = $"<{msgType}> Message {direction} {nodeName} {remoteIp}";
            string filename = GetFileName(nodeName);

            return WriteLineWithRetryAsync(filename, FormatLine(title, streamMessage) + Environment.NewLine, ct);
        }

        private static string FormatLine(string message, string details)
        {
            string timestamp = DateTime.Now.ToString("[dd MMM yyyy HH:mm:ss.fff] ");
            return string.IsNullOrEmpty(details)
                ? timestamp + message
                : timestamp + message + Environment.NewLine + details;
        }

        private void WriteLineWithRetry(string filename, string line)
        {
            int? retries = SdkConfig.Settings.RetryPolicy.MaxRetryLog;
            Exception? lastError = null;

            for (int attempt = 0; attempt < retries; attempt++)
            {
                _fileLock.Wait();
                try
                {
                    using var writer = new StreamWriter(filename, append: true);
                    writer.WriteLine(line);
                    return;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    Thread.Sleep(50);
                }
                finally
                {
                    _fileLock.Release();
                }
            }

            throw new IOException($"Failed to write trace log to '{filename}' after {retries} attempts.", lastError);
        }

        private async Task WriteLineWithRetryAsync(string filename, string line, CancellationToken ct)
        {
            int? retries = SdkConfig.Settings.RetryPolicy.MaxRetryLog;
            Exception? lastError = null;

            for (int attempt = 0; attempt < retries; attempt++)
            {
                await _fileLock.WaitAsync(ct);
                try
                {
                    await using var writer = new StreamWriter(filename, append: true);
                    await writer.WriteLineAsync(line);
                    return;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    await Task.Delay(50, ct);
                }
                finally
                {
                    _fileLock.Release();
                }
            }

            throw new IOException($"Failed to write trace log to '{filename}' after {retries} attempts.", lastError);
        }

        private string GetTraceDirectory()
        {
            string appFolder = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? _appName
                : _appName.AdjustFileLinux();

            return Path.Combine(_traceDir, appFolder);
        }

        private string GetFileName(string traceName)
        {
            string name = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? traceName
                : traceName.AdjustFileLinux();

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HH");

            return Path.Combine(GetTraceDirectory(), $"{name}_{timestamp}.log");
        }
    }
}