using Microsoft.Extensions.Logging;

namespace SyncNetPro.Sdk.Nodes;

/// <summary>Pelapor status tanpa database (mode JSON/SimCore): hanya mencatat ke log.</summary>
public sealed class LoggingNodeStatusReporter(ILogger<LoggingNodeStatusReporter> logger) : INodeStatusReporter
{
    /// <inheritdoc />
    public Task ReportApplicationAsync(string appName, bool up, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Status aplikasi {AppName}: {Status}", appName, up ? "UP" : "DOWN");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ReportCoreChannelAsync(string nodeName, CoreChannelDirection direction, bool up, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Status kanal {Direction} {Node}: {Status}", direction, nodeName, up ? "UP" : "DOWN");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ReportRemoteNodeAsync(string nodeName, bool up, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Status remote {Node}: {Status}", nodeName, up ? "UP" : "DOWN");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ReportConnectionAsync(string connectionName, bool up, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Status koneksi {Connection}: {Status}", connectionName, up ? "UP" : "DOWN");
        return Task.CompletedTask;
    }
}
