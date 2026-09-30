using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SyncNetPro.Sdk.Nodes;

/// <summary>Implementasi <see cref="INodeRegistry"/> berbasis snapshot immutable.</summary>
public sealed class NodeRegistry(
    INodeConfigurationSource source,
    IOptions<SyncNetOptions> options,
    ILogger<NodeRegistry> logger) : INodeRegistry
{
    private volatile NodeConfiguration _current = NodeConfiguration.Empty;

    /// <inheritdoc />
    public NodeConfiguration Current => _current;

    /// <inheritdoc />
    public bool TryGetNode(string nodeName, [NotNullWhen(true)] out NodeInfo? node)
    {
        node = _current.Nodes.FirstOrDefault(n => string.Equals(n.Name, nodeName, StringComparison.Ordinal));
        return node is not null;
    }

    /// <inheritdoc />
    public IReadOnlyList<RemoteConnectionInfo> GetConnections(string nodeName) =>
        [.. _current.Connections.Where(c => string.Equals(c.NodeName, nodeName, StringComparison.Ordinal))];

    /// <inheritdoc />
    public async Task<NodeConfiguration> ReloadAsync(CancellationToken cancellationToken)
    {
        string appName = options.Value.AppName;
        NodeConfiguration configuration = await source.LoadAsync(appName, cancellationToken).ConfigureAwait(false);

        var duplicates = configuration.Nodes.GroupBy(n => n.Name, StringComparer.Ordinal).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Count > 0)
        {
            throw new InvalidDataException($"Nama node duplikat untuk aplikasi {appName}: {string.Join(", ", duplicates)}");
        }

        if (configuration.Nodes.Count == 0)
        {
            logger.LogWarning("Tidak ada node untuk aplikasi {AppName}; tambahkan node dari configuration console", appName);
        }

        _current = configuration;
        logger.LogInformation("Konfigurasi dimuat: {Nodes} node, {Connections} koneksi", configuration.Nodes.Count, configuration.Connections.Count);
        return configuration;
    }
}
