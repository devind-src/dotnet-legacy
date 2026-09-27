using Microsoft.Extensions.Hosting;
using SyncNet.Common;
using SyncNet.Helpers;
using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SyncNet.Services
{
    public class LogWriterService : BackgroundService
    {
        private readonly Channel<string> _logChannel;

        public LogWriterService(Channel<string> logChannel)
        {
            _logChannel = logChannel;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // make sure directory exist
            string fileName = AppConfig.APPNAME.AdjustFileName();
            string pathDir = Path.Combine(AppConfig.TraceDir, fileName);
            if (Directory.Exists(pathDir) == false) Directory.CreateDirectory(pathDir);

            // set filename
            string pathFile = Path.Combine(pathDir, $"{AppConfig.APPNAME.AdjustFileName()}_{DateTime.Now:yyyyMMdd}.log");

            await foreach (var log in _logChannel.Reader.ReadAllAsync(stoppingToken))
            {
                // buka file hanya saat ada log
                using var fs = new FileStream(
                    pathFile,
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.ReadWrite); // proses lain bisa baca
                using var writer = new StreamWriter(fs);

                await writer.WriteLineAsync(log);
                await writer.FlushAsync(); // segera flush agar bisa dibaca
            }
        }
    }
}
