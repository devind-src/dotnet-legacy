using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using SyncNetPro.Sdk.Tracing;

namespace SyncNetPro.Sdk.Logging;

/// <summary>
/// Log file harian <c>{LogDir}/{app}_{yyyy-MM-dd}.log</c> dengan format SDK lama
/// (<c>[HH:mm:ss] pesan</c> lalu detail/exception dalam <c>{ }</c>). Log Information ke atas
/// juga diteruskan ke Log Services sebagai trace info (perilaku <c>AppProcessor.Logger</c> lama).
/// Penulisan dilakukan di background agar tidak memblokir pemanggil.
/// </summary>
[ProviderAlias("SyncNetFile")]
public sealed class SyncNetFileLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    internal const string NodeScopeKey = "SyncNetNode";

    private readonly string _directory;
    private readonly string _fileName;
    private readonly TimeProvider _time;
    private readonly Func<ITraceWriter?> _trace;
    private readonly bool _forwardToTrace;
    private readonly Channel<(DateTime Time, string Text)> _queue = Channel.CreateUnbounded<(DateTime, string)>(new UnboundedChannelOptions { SingleReader = true });
    private readonly Task _writer;
    private readonly ConcurrentDictionary<string, Logger> _loggers = new(StringComparer.Ordinal);
    private IExternalScopeProvider? _scopes;

    /// <summary>Membuat provider.</summary>
    public SyncNetFileLoggerProvider(string directory, string appName, bool legacyWindowsNames, bool forwardToTrace, Func<ITraceWriter?> trace, TimeProvider time)
    {
        _directory = directory;
        _fileName = LogFileNames.Normalize(appName, legacyWindowsNames);
        _forwardToTrace = forwardToTrace;
        _trace = trace;
        _time = time;
        _writer = Task.Run(WriteLoopAsync);
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => _loggers.GetOrAdd(categoryName, c => new Logger(this, c));

    /// <inheritdoc />
    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopes = scopeProvider;

    /// <inheritdoc />
    public void Dispose()
    {
        _queue.Writer.TryComplete();
        _writer.Wait(TimeSpan.FromSeconds(5));
    }

    private async Task WriteLoopAsync()
    {
        await foreach ((DateTime time, string text) in _queue.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            string file = Path.Combine(_directory, $"{_fileName}_{time.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.log");
            for (int attempt = 0; attempt < 10; attempt++)
            {
                try
                {
                    Directory.CreateDirectory(_directory);
                    await File.AppendAllTextAsync(file, text).ConfigureAwait(false);
                    break;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    await Task.Delay(50).ConfigureAwait(false);
                }
            }
        }
    }

    private sealed class Logger(SyncNetFileLoggerProvider provider, string category) : ILogger
    {
        private readonly bool _forward = provider._forwardToTrace && !category.StartsWith("SyncNetPro.Sdk.Tracing", StringComparison.Ordinal);

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => provider._scopes?.Push(state);

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information && logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            string message = formatter(state, exception);
            string? detail = exception?.ToString();
            DateTime now = provider._time.GetLocalNow().LocalDateTime;

            var sb = new StringBuilder();
            sb.Append(now.ToString("[HH:mm:ss] ", CultureInfo.InvariantCulture));
            if (logLevel >= LogLevel.Warning) sb.Append(logLevel == LogLevel.Warning ? "WARN " : "ERROR ");
            sb.Append(message).Append('\n');
            if (!string.IsNullOrEmpty(detail)) sb.Append('{').Append(detail).Append("}\n\n");
            provider._queue.Writer.TryWrite((now, sb.ToString()));

            if (_forward && provider._trace() is { IsEnabled: true } trace)
            {
                trace.Info(FindNode(), message, detail);
            }
        }

        private string FindNode()
        {
            string node = string.Empty;
            provider._scopes?.ForEachScope(
                (scope, _) =>
                {
                    if (scope is IEnumerable<KeyValuePair<string, object?>> pairs)
                    {
                        foreach (KeyValuePair<string, object?> pair in pairs)
                        {
                            if (pair.Key == NodeScopeKey && pair.Value is string value) node = value;
                        }
                    }
                },
                (object?)null);
            return node;
        }
    }
}
