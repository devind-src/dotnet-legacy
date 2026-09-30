using Microsoft.Extensions.Logging;

namespace SyncNetPro.Sdk.Logging;

/// <summary>Helper logging SyncNet.</summary>
public static class SyncNetLoggerExtensions
{
    /// <summary>
    /// Scope node: log di dalam scope ini diteruskan ke Log Services dengan <c>FileName</c> = nama node
    /// (setara <c>AppProcessor.Logger(msg, detail, NodeName)</c> lama).
    /// </summary>
    public static IDisposable? BeginNodeScope(this ILogger logger, string nodeName)
    {
        ArgumentNullException.ThrowIfNull(logger);
        return logger.BeginScope(new Dictionary<string, object?> { [SyncNetFileLoggerProvider.NodeScopeKey] = nodeName });
    }
}
