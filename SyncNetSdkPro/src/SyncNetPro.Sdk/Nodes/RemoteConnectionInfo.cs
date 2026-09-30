namespace SyncNetPro.Sdk.Nodes;

/// <summary>Protokol koneksi eksternal (<c>sw_connections.protocol</c>).</summary>
public enum ConnectionProtocol
{
    /// <summary>0: header 2 byte, exclude.</summary>
    Tcp2ByteExcludeHeader = 0,

    /// <summary>1: header 2 byte, include.</summary>
    Tcp2ByteIncludeHeader = 1,

    /// <summary>2: header 4 byte, exclude.</summary>
    Tcp4ByteExcludeHeader = 2,

    /// <summary>3: header 4 byte, include.</summary>
    Tcp4ByteIncludeHeader = 3,

    /// <summary>4: framing kustom (codec dari interface).</summary>
    TcpHeaderCustom = 4,

    /// <summary>5: tanpa header.</summary>
    TcpHeaderNone = 5,

    /// <summary>6: message queue.</summary>
    MessageQueue = 6,

    /// <summary>7: HTTP / web service.</summary>
    WebService = 7,

    /// <summary>8: kustom.</summary>
    Custom = 8,
}

/// <summary>Peran koneksi (<c>sw_connections.conn_type</c>).</summary>
public enum ConnectionRole
{
    /// <summary>0: interface menjadi server.</summary>
    Server = 0,

    /// <summary>1: interface menjadi klien.</summary>
    Client = 1,
}

/// <summary>Format header TCP (<c>sw_connections.tcp_header_format</c>).</summary>
public enum TcpHeaderFormat
{
    /// <summary>0: ASCII / biner.</summary>
    Ascii = 0,

    /// <summary>1: BCD.</summary>
    Bcd = 1,
}

/// <summary>Koneksi ke sistem eksternal (baris <c>sw_connections</c>). Dipakai transport remote (fase 3).</summary>
public sealed record RemoteConnectionInfo
{
    /// <summary><c>conn_name</c>.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary><c>node_id</c>.</summary>
    public int NodeId { get; init; }

    /// <summary><c>node_name</c> (dari join <c>sw_nodes</c>).</summary>
    public string NodeName { get; init; } = string.Empty;

    /// <summary><c>protocol</c>.</summary>
    public ConnectionProtocol Protocol { get; init; }

    /// <summary><c>conn_type</c>.</summary>
    public ConnectionRole Role { get; init; }

    /// <summary><c>tcp_header_format</c>.</summary>
    public TcpHeaderFormat HeaderFormat { get; init; }

    /// <summary><c>tcp_hi_lo</c> ≠ "0": big-endian.</summary>
    public bool HeaderHighLow { get; init; } = true;

    /// <summary><c>ip_address</c>: host remote (klien) atau alamat bind (server).</summary>
    public string? Host { get; init; }

    /// <summary><c>port</c>.</summary>
    public int Port { get; init; }

    /// <summary><c>max_conn</c>.</summary>
    public int MaxConnections { get; init; }

    /// <summary><c>retry_delay</c> dalam detik.</summary>
    public int RetryDelaySeconds { get; init; }

    /// <summary><c>always_connected</c> = "1": persistent.</summary>
    public bool AlwaysConnected { get; init; } = true;

    /// <summary><c>one_socket_only</c> = "1".</summary>
    public bool OneSocketOnly { get; init; }

    /// <summary><c>ws_url</c>.</summary>
    public string? WsUrl { get; init; }

    /// <summary><c>ws_header</c>.</summary>
    public string? WsHeader { get; init; }

    /// <summary><c>ws_method</c>.</summary>
    public string? WsMethod { get; init; }

    /// <summary><c>ws_content</c>.</summary>
    public string? WsContent { get; init; }

    /// <summary><c>ws_ipsource</c>.</summary>
    public string? WsIpSource { get; init; }

    /// <summary><c>ws_key</c>.</summary>
    public string? WsKey { get; init; }

    /// <summary><c>ws_user</c>.</summary>
    public string? WsUser { get; init; }

    /// <summary><c>ws_pswd</c>.</summary>
    public string? WsPassword { get; init; }

    /// <summary><c>ws_proxy_url</c>.</summary>
    public string? WsProxyUrl { get; init; }

    /// <summary><c>ws_proxy_port</c>.</summary>
    public int WsProxyPort { get; init; }

    /// <summary><c>queue_inbox</c>.</summary>
    public string? QueueInbox { get; init; }

    /// <summary><c>queue_outbox</c>.</summary>
    public string? QueueOutbox { get; init; }
}
