using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using SyncNetApi.Options;

namespace SyncNetApi.Logging
{
    /// <summary>Minimal file-based ILoggerProvider — appends Error+ log entries (by default) to
    /// a single configured file so internal errors can be diagnosed without console access.
    /// Folder/FileName/MinLevel come from FileLoggingOptions (appsettings.json "Logging:File").
    /// Hooking this into the standard logging pipeline is enough on its own: ASP.NET Core's
    /// built-in UseExceptionHandler() middleware already logs every unhandled exception via
    /// ILogger at Error level — no extra exception-handling code needed for it to be captured.
    /// <br/><br/>
    /// Hanya inti error yang ditulis, satu baris per entri: baris pertama pesan (dipotong),
    /// tipe + pesan exception terdalam, dan satu lokasi kode SyncNetApi tempat error terjadi.
    /// Stack trace lengkap dan teks multi-baris (EF Core menyisipkan exception.ToString() dan
    /// SQL ke dalam pesannya sendiri) sengaja dibuang.</summary>
    public sealed class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _filePath;
        private readonly LogLevel _minLevel;
        private readonly object _writeLock = new();

        public FileLoggerProvider(FileLoggingOptions options)
        {
            try
            {
                Directory.CreateDirectory(options.Folder);
            }
            catch
            {
                // A misconfigured/inaccessible log folder must not prevent the app from
                // starting — Log() below already swallows per-write failures the same way.
            }

            _filePath = Path.Combine(options.Folder, options.FileName);
            _minLevel = options.MinLevel;
        }

        public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, _filePath, _minLevel, _writeLock);

        public void Dispose() { }

        private sealed class FileLogger : ILogger
        {
            private const int MaxMessageLength = 500;
            private const string AppNamespace = "SyncNetApi.";

            private readonly string _categoryName;
            private readonly string _filePath;
            private readonly LogLevel _minLevel;
            private readonly object _writeLock;

            public FileLogger(string categoryName, string filePath, LogLevel minLevel, object writeLock)
            {
                _categoryName = categoryName;
                _filePath = filePath;
                _minLevel = minLevel;
                _writeLock = writeLock;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel && logLevel != LogLevel.None;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel)) return;

                var line = new StringBuilder()
                    .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                    .Append(" [").Append(logLevel).Append("] ")
                    .Append(_categoryName)
                    .Append(" - ").Append(FirstLine(formatter(state, null)));

                if (exception != null)
                {
                    // exception terdalam (mis. PostgresException) membawa penyebab sebenarnya
                    var baseException = exception.GetBaseException();
                    line.Append(" | ").Append(baseException.GetType().Name)
                        .Append(": ").Append(FirstLine(baseException.Message));

                    var location = AppLocation(exception);
                    if (location != null)
                        line.Append(" @ ").Append(location);
                }

                line.Append(Environment.NewLine);

                lock (_writeLock)
                {
                    try
                    {
                        File.AppendAllText(_filePath, line.ToString(), Encoding.UTF8);
                    }
                    catch
                    {
                        // Logging must never crash the app it's trying to diagnose.
                    }
                }
            }

            // baris pertama saja, dipotong supaya satu entri tidak memuat SQL/stack trace panjang
            private static string FirstLine(string? text)
            {
                if (string.IsNullOrWhiteSpace(text)) return string.Empty;

                var first = text.Split('\n', 2)[0].TrimEnd('\r').Trim();
                return first.Length <= MaxMessageLength ? first : first[..MaxMessageLength] + "…";
            }

            // frame pertama milik kode SyncNetApi (dari exception terdalam ke luar), mis.
            // "ProductPostpaidFeeService.CreateAsync". Null bila error murni dari framework.
            private static string? AppLocation(Exception exception)
            {
                for (var ex = exception.GetBaseException(); ; )
                {
                    var frame = new StackTrace(ex, fNeedFileInfo: false).GetFrames()
                        .Select(f => f.GetMethod())
                        .FirstOrDefault(m => m?.DeclaringType?.FullName?.StartsWith(AppNamespace) == true);

                    if (frame != null)
                    {
                        // method async berada di state machine "<CreateAsync>d__5": ambil nama aslinya
                        var type = frame.DeclaringType!;
                        string method = frame.Name;
                        if (type.Name.StartsWith('<') && type.DeclaringType != null)
                        {
                            method = type.Name[1..type.Name.IndexOf('>')];
                            type = type.DeclaringType;
                        }
                        return $"{type.Name}.{method}";
                    }

                    if (ReferenceEquals(ex, exception)) return null;
                    ex = OuterOf(exception, ex);
                }
            }

            // exception pembungkus langsung dari target (bergerak dari dalam ke luar)
            private static Exception OuterOf(Exception root, Exception target)
            {
                var current = root;
                while (current.InnerException != null && !ReferenceEquals(current.InnerException, target))
                    current = current.InnerException;
                return current;
            }
        }
    }
}
