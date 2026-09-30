using System.Globalization;
using SyncNetPro.Sdk.Logging;

namespace SyncNetPro.Sdk.Tracing;

/// <summary>
/// Trace ke file (sink utama untuk mode file, dan fallback sink lain) —
/// <c>{TraceDir}/{app}/{node}_{yyyyMMdd_HH}.log</c>, format baris seperti SDK lama.
/// </summary>
public sealed class FileTraceSink(string directory, bool legacyWindowsNames = false) : ITraceSink
{
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <inheritdoc />
    public string Name => $"file ({directory})";

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public async Task<bool> TrySendAsync(TraceRecord record, string json, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);

        string appFolder = LogFileNames.Normalize(record.AppName ?? "syncnet", legacyWindowsNames);
        string logName = LogFileNames.Normalize(record.FileName ?? "trace", legacyWindowsNames);
        string folder = Path.Combine(directory, appFolder);
        string file = Path.Combine(folder, $"{logName}_{record.Datetime.ToString("yyyyMMdd_HH", CultureInfo.InvariantCulture)}.log");

        string stamp = record.Datetime.ToString("[dd MMM yyyy HH:mm:ss.fff] ", CultureInfo.InvariantCulture);
        string detail = record.Detail?.ToString() ?? string.Empty;
        string text = detail.Length == 0
            ? $"{stamp}{record.Title}\n"
            : $"{stamp}{record.Title}\n{detail}\n\n";

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(folder);
            await File.AppendAllTextAsync(file, text, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _lock.Dispose();
        return ValueTask.CompletedTask;
    }
}
