using Microsoft.Extensions.Diagnostics.HealthChecks;
using SyncNetPro.Sdk.Core;

namespace SyncNetPro.Sdk.Hosting;

/// <summary>Health check kanal Core: Healthy bila semua terkoneksi, Degraded bila sebagian, Unhealthy bila tidak ada.</summary>
public sealed class CoreChannelHealthCheck(CoreChannelManager channels) : IHealthCheck
{
    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var all = channels.Channels;
        var data = all.ToDictionary(c => $"{c.Node}/{c.Direction}", c => (object)(c.Connected ? "connected" : $"disconnected ({c.Target})"));
        int connected = all.Count(c => c.Connected);

        HealthCheckResult result = all.Count == 0 ? HealthCheckResult.Degraded("Tidak ada kanal Core.", data: data)
            : connected == all.Count ? HealthCheckResult.Healthy($"{connected} kanal Core terkoneksi.", data)
            : connected > 0 ? HealthCheckResult.Degraded($"{all.Count - connected} dari {all.Count} kanal Core terputus.", data: data)
            : HealthCheckResult.Unhealthy("Semua kanal Core terputus.", data: data);
        return Task.FromResult(result);
    }
}
