using System.Net;
using Microsoft.Extensions.Options;

namespace SyncNetPro.Sdk.Nodes;

/// <summary>Konfigurasi node dari appsettings (<c>SyncNet:Nodes</c>, <c>SyncNet:Connections</c>) untuk pengembangan/SimCore.</summary>
public sealed class JsonNodeConfigurationSource(IOptionsMonitor<SyncNetOptions> options) : INodeConfigurationSource
{
    /// <inheritdoc />
    public Task<NodeConfiguration> LoadAsync(string appName, CancellationToken cancellationToken)
    {
        SyncNetOptions o = options.CurrentValue;
        DnsEndPoint? logServices = o.Trace.LogServicesHost is { Length: > 0 } host && o.Trace.LogServicesPort is > 0
            ? new DnsEndPoint(host, o.Trace.LogServicesPort.Value)
            : null;

        return Task.FromResult(new NodeConfiguration([.. o.Nodes], [.. o.Connections], o.Command.Port, logServices));
    }
}
