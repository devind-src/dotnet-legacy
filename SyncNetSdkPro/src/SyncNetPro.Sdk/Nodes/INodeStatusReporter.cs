namespace SyncNetPro.Sdk.Nodes;

/// <summary>Arah kanal Core dari sudut pandang interface.</summary>
public enum CoreChannelDirection
{
    /// <summary>Eksternal → Core (Core SourceNode, <c>port_in</c>, kolom <c>conn_in</c>).</summary>
    Inbound,

    /// <summary>Core → eksternal (Core SinkNode, <c>port_out</c>, kolom <c>conn_out</c>).</summary>
    Outbound,
}

/// <summary>Pelapor status ke database/monitoring (kontrak DB dok. 02 §6).</summary>
public interface INodeStatusReporter
{
    /// <summary>Status aplikasi (<c>sw_app.status</c>) dan semua node (<c>sw_nodes.status</c>).</summary>
    Task ReportApplicationAsync(string appName, bool up, CancellationToken cancellationToken = default);

    /// <summary>Status kanal Core (<c>sw_nodes.conn_in</c> / <c>conn_out</c>).</summary>
    Task ReportCoreChannelAsync(string nodeName, CoreChannelDirection direction, bool up, CancellationToken cancellationToken = default);

    /// <summary>Status sisi remote node (<c>sw_nodes.remote</c>, <c>last_connected/last_disconnected</c>).</summary>
    Task ReportRemoteNodeAsync(string nodeName, bool up, CancellationToken cancellationToken = default);

    /// <summary>Status koneksi eksternal (<c>sw_connections.remote</c>).</summary>
    Task ReportConnectionAsync(string connectionName, bool up, CancellationToken cancellationToken = default);
}
