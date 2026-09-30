using System.Globalization;
using System.Net;
using Dapper;
using Npgsql;

namespace SyncNetPro.Sdk.Nodes;

/// <summary>
/// Konfigurasi node dari database Core — query setara <c>DbMgr.GetNodes/GetConnection/GetPortCommand/GetEndPointLogServices</c>
/// SDK lama (read-only).
/// </summary>
public sealed class PostgresNodeConfigurationSource(NpgsqlDataSource dataSource) : INodeConfigurationSource
{
    internal const string NodesSql = """
        SELECT node_id, node_name, app_name, port_in, port_out, inst_id, auto_signon, auto_reversal,
               keychange_timer, echo_timer, request_timeout, advice_timeout, category, pin_translate,
               saf_limit, sensitive_data, parameter
        FROM sw_nodes
        WHERE app_name = @app_name
        """;

    internal const string ConnectionsSql = """
        SELECT SC.node_id, SN.node_name, SC.conn_name, SC.protocol, SC.tcp_header_format, SC.tcp_hi_lo,
               SC.conn_type, SC.ip_address, SC.port, SC.queue_inbox, SC.queue_outbox, SC.max_conn,
               SC.always_connected, SC.ws_url, SC.ws_header, SC.ws_method, SC.ws_content, SC.ws_ipsource,
               SC.ws_proxy_url, SC.ws_proxy_port, SC.ws_key, SC.ws_user, SC.ws_pswd, SC.one_socket_only,
               SC.retry_delay
        FROM sw_connections SC
        INNER JOIN sw_nodes SN ON SN.node_id = SC.node_id
        WHERE SN.app_name = @app_name
        """;

    internal const string CommandPortSql = "SELECT command_port FROM sw_app WHERE app_name = @app_name";

    internal const string LogServicesSql = "SELECT host, command_port FROM sw_app WHERE app_name = 'Log Services'";

    /// <inheritdoc />
    public async Task<NodeConfiguration> LoadAsync(string appName, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var args = new { app_name = appName };

        var nodes = (await connection.QueryAsync(new CommandDefinition(NodesSql, args, cancellationToken: cancellationToken)).ConfigureAwait(false))
            .Select(r => (IDictionary<string, object?>)r)
            .Select(ToNode)
            .ToList();

        var connections = (await connection.QueryAsync(new CommandDefinition(ConnectionsSql, args, cancellationToken: cancellationToken)).ConfigureAwait(false))
            .Select(r => (IDictionary<string, object?>)r)
            .Select(ToConnection)
            .ToList();

        object? port = await connection.ExecuteScalarAsync(new CommandDefinition(CommandPortSql, args, cancellationToken: cancellationToken)).ConfigureAwait(false);

        var log = (IDictionary<string, object?>?)await connection.QueryFirstOrDefaultAsync(new CommandDefinition(LogServicesSql, cancellationToken: cancellationToken)).ConfigureAwait(false);
        DnsEndPoint? logServices = log is not null && ToInt(log["command_port"]) is > 0 and var logPort
            ? new DnsEndPoint(string.IsNullOrWhiteSpace(Str(log["host"])) ? "127.0.0.1" : Str(log["host"])!.Trim(), logPort)
            : null;

        int commandPort = ToInt(port);
        return new NodeConfiguration(nodes, connections, commandPort > 0 ? commandPort : null, logServices);
    }

    internal static NodeInfo ToNode(IDictionary<string, object?> r) => new()
    {
        Id = ToInt(r["node_id"]),
        Name = Str(r["node_name"]) ?? string.Empty,
        Category = Str(r["category"]) switch
        {
            "1" => NodeCategory.Merchant,
            "2" => NodeCategory.BillerIssuer,
            _ => NodeCategory.Both,
        },
        InstitutionId = Str(r["inst_id"]),
        PortIn = ToInt(r["port_in"]),
        PortOut = ToInt(r["port_out"]),
        AutoSignOn = Flag(r["auto_signon"]),
        AutoReversal = Str(r["auto_reversal"]),
        KeyChangeTimerMinutes = ToInt(r["keychange_timer"]),
        EchoTimerMinutes = ToInt(r["echo_timer"]),
        RequestTimeoutSeconds = ToInt(r["request_timeout"]),
        AdviceTimeoutSeconds = ToInt(r["advice_timeout"]),
        PinTranslate = Flag(r["pin_translate"]),
        SafLimit = ToInt(r["saf_limit"]),
        ProtectSensitiveData = Flag(r["sensitive_data"]),
        Parameter = Str(r["parameter"]),
    };

    internal static RemoteConnectionInfo ToConnection(IDictionary<string, object?> r) => new()
    {
        Name = Str(r["conn_name"]) ?? string.Empty,
        NodeId = ToInt(r["node_id"]),
        NodeName = Str(r["node_name"]) ?? string.Empty,
        Protocol = (ConnectionProtocol)ToInt(r["protocol"]),
        Role = ToInt(r["conn_type"]) == 1 ? ConnectionRole.Client : ConnectionRole.Server,
        HeaderFormat = ToInt(r["tcp_header_format"]) == 1 ? TcpHeaderFormat.Bcd : TcpHeaderFormat.Ascii,
        HeaderHighLow = Str(r["tcp_hi_lo"]) != "0",
        Host = Str(r["ip_address"]),
        Port = ToInt(r["port"]),
        MaxConnections = ToInt(r["max_conn"]),
        RetryDelaySeconds = ToInt(r["retry_delay"]),
        AlwaysConnected = Flag(r["always_connected"]),
        OneSocketOnly = Flag(r["one_socket_only"]),
        WsUrl = Str(r["ws_url"]),
        WsHeader = Str(r["ws_header"]),
        WsMethod = Str(r["ws_method"]),
        WsContent = Str(r["ws_content"]),
        WsIpSource = Str(r["ws_ipsource"]),
        WsKey = Str(r["ws_key"]),
        WsUser = Str(r["ws_user"]),
        WsPassword = Str(r["ws_pswd"]),
        WsProxyUrl = Str(r["ws_proxy_url"]),
        WsProxyPort = ToInt(r["ws_proxy_port"]),
        QueueInbox = Str(r["queue_inbox"]),
        QueueOutbox = Str(r["queue_outbox"]),
    };

    /// <summary>Konversi toleran seperti <c>NbConvert.ToInt</c> lama: nilai kosong/tidak valid → 0.</summary>
    internal static int ToInt(object? value) => value switch
    {
        null or DBNull => 0,
        int i => i,
        short s => s,
        long l => (int)l,
        decimal d => (int)d,
        string str => int.TryParse(str.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : 0,
        IConvertible c => c.ToInt32(CultureInfo.InvariantCulture),
        _ => 0,
    };

    internal static string? Str(object? value) => value switch
    {
        null or DBNull => null,
        string s => s,
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString(),
    };

    private static bool Flag(object? value) => Str(value)?.Trim() == "1";
}
