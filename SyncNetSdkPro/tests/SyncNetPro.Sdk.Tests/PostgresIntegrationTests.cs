using Dapper;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using SyncNetPro.Sdk.Nodes;
using SyncNetPro.Sdk.Tests.Infrastructure;

namespace SyncNetPro.Sdk.Tests;

/// <summary>
/// Integrasi dengan PostgreSQL nyata. Dijalankan bila env <c>SYNCNET_TEST_PG</c> berisi connection string;
/// tabel dibuat dengan tipe kolom sesuai entitas SyncNetWebApi (port/category/status bertipe teks).
/// </summary>
public class PostgresIntegrationTests
{
    private static readonly string? ConnectionString = Environment.GetEnvironmentVariable("SYNCNET_TEST_PG");

    private const string Schema = """
        CREATE TABLE IF NOT EXISTS sw_app (
            app_name varchar(50) PRIMARY KEY, host varchar(50), app_type varchar(10), command_port varchar(10),
            path varchar(200), status varchar(5), last_update timestamp);
        CREATE TABLE IF NOT EXISTS sw_nodes (
            node_id serial PRIMARY KEY, node_name varchar(50) UNIQUE NOT NULL, app_name varchar(50),
            port_in varchar(10), port_out varchar(10), parameter varchar(200), inst_id varchar(20),
            auto_signon varchar(1), auto_reversal varchar(1), keychange_timer smallint, echo_timer smallint,
            request_timeout smallint, advice_timeout smallint, pin_translate varchar(1), remote smallint,
            conn_in smallint, conn_out smallint, saf_limit int, sensitive_data int, category varchar(1),
            status varchar(5), last_connected timestamp, last_disconnected timestamp);
        CREATE TABLE IF NOT EXISTS sw_connections (
            conn_name varchar(50) PRIMARY KEY, node_id int, protocol varchar(2), tcp_header_format varchar(2),
            conn_type varchar(2), ip_address varchar(50), port varchar(10), max_conn int, retry_delay int,
            always_connected varchar(1), queue_inbox varchar(50), queue_outbox varchar(50), ws_url varchar(200),
            ws_user varchar(50), ws_pswd varchar(50), remote smallint, last_connected timestamp,
            last_disconnected timestamp, ws_header varchar(200), ws_method varchar(10), ws_content varchar(50),
            ws_ipsource varchar(50), ws_key varchar(100), tcp_hi_lo smallint, ws_proxy_url varchar(100),
            ws_proxy_port varchar(10), one_socket_only varchar(1));
        """;

    private static async Task<(NpgsqlDataSource Source, string App)> SeedAsync(int portIn, int portOut, string category = "2")
    {
        NpgsqlDataSource source = NpgsqlDataSource.Create(ConnectionString!);
        await using NpgsqlConnection db = await source.OpenConnectionAsync();
        await db.ExecuteAsync(Schema);

        string app = "IT " + Guid.NewGuid().ToString("N")[..8];
        string node = "N_" + Guid.NewGuid().ToString("N")[..8];
        await db.ExecuteAsync("INSERT INTO sw_app(app_name, host, command_port, status) VALUES (@app, 'localhost', '0', '0')", new { app });
        await db.ExecuteAsync("INSERT INTO sw_app(app_name, host, command_port) VALUES ('Log Services', '10.9.9.9', '7777') ON CONFLICT DO NOTHING");
        int nodeId = await db.ExecuteScalarAsync<int>("""
            INSERT INTO sw_nodes(node_name, app_name, port_in, port_out, inst_id, auto_signon, auto_reversal, keychange_timer,
                echo_timer, request_timeout, advice_timeout, pin_translate, saf_limit, sensitive_data, category, parameter)
            VALUES (@node, @app, @portIn, @portOut, '441', '1', '1', 0, 5, 25, 40, '0', 3, 1, @category, 'p=1')
            RETURNING node_id
            """, new { node, app, portIn = portIn.ToString(System.Globalization.CultureInfo.InvariantCulture), portOut = portOut.ToString(System.Globalization.CultureInfo.InvariantCulture), category });
        await db.ExecuteAsync("""
            INSERT INTO sw_connections(conn_name, node_id, protocol, tcp_header_format, conn_type, ip_address, port, max_conn,
                retry_delay, always_connected, tcp_hi_lo, one_socket_only, ws_url, ws_method, ws_proxy_port)
            VALUES (@conn, @nodeId, '0', '1', '1', '10.0.0.5', '5000', 10, 7, '1', 0, '1', 'http://biller/api', 'POST', '')
            """, new { conn = node + "_C1", nodeId });
        return (source, app);
    }

    [Fact]
    public async Task Loads_nodes_connections_and_ports_from_database()
    {
        Assert.SkipWhen(string.IsNullOrEmpty(ConnectionString), "Set SYNCNET_TEST_PG untuk menjalankan test PostgreSQL");
        (NpgsqlDataSource source, string app) = await SeedAsync(9101, 9102);
        await using NpgsqlDataSource _ = source;

        NodeConfiguration config = await new PostgresNodeConfigurationSource(source).LoadAsync(app, CancellationToken.None);

        NodeInfo node = Assert.Single(config.Nodes);
        Assert.Equal(NodeCategory.BillerIssuer, node.Category);
        Assert.Equal(9101, node.PortIn);
        Assert.Equal(9102, node.PortOut);
        Assert.Equal(25, node.RequestTimeoutSeconds);
        Assert.Equal(40, node.AdviceTimeoutSeconds);
        Assert.Equal(5, node.EchoTimerMinutes);
        Assert.True(node.AutoSignOn);
        Assert.True(node.ProtectSensitiveData);
        Assert.Equal("441", node.InstitutionId);
        Assert.Equal("p=1", node.Parameter);

        RemoteConnectionInfo connection = Assert.Single(config.Connections);
        Assert.Equal(node.Name, connection.NodeName);
        Assert.Equal(ConnectionProtocol.Tcp2ByteExcludeHeader, connection.Protocol);
        Assert.Equal(ConnectionRole.Client, connection.Role);
        Assert.Equal(TcpHeaderFormat.Bcd, connection.HeaderFormat);
        Assert.False(connection.HeaderHighLow);
        Assert.Equal("10.0.0.5", connection.Host);
        Assert.Equal(5000, connection.Port);
        Assert.True(connection.OneSocketOnly);
        Assert.Equal(0, connection.WsProxyPort);

        Assert.Null(config.CommandPort);
        Assert.Equal("10.9.9.9", config.LogServices!.Host);
        Assert.Equal(7777, config.LogServices.Port);
    }

    [Fact]
    public async Task Database_mode_reports_status_like_legacy_sdk()
    {
        Assert.SkipWhen(string.IsNullOrEmpty(ConnectionString), "Set SYNCNET_TEST_PG untuk menjalankan test PostgreSQL");
        await using FakeCore core = await FakeCore.StartAsync();
        (NpgsqlDataSource source, string app) = await SeedAsync(core.SourcePort, core.SinkPort);
        await using NpgsqlDataSource _ = source;

        var host = await TestHost<DefaultInterface>.StartAsync([], o =>
        {
            o.AppName = app;
            o.NodeSource = NodeSourceKind.Database;
            o.Database.ConnectionString = ConnectionString;
            o.Trace.Sink = TraceSinkKind.File;
        });
        await using NpgsqlConnection db = await source.OpenConnectionAsync();
        try
        {
            await Wait.UntilAsync(() => core.SinkConnected, "kanal outbound terkoneksi");
            await Wait.UntilAsync(() => db.ExecuteScalar<short?>("SELECT conn_out FROM sw_nodes WHERE app_name=@app", new { app }) == 1, "conn_out = 1");
            Assert.Equal("1", await db.ExecuteScalarAsync<string>("SELECT status FROM sw_app WHERE app_name=@app", new { app }));
            Assert.Equal("1", await db.ExecuteScalarAsync<string>("SELECT status FROM sw_nodes WHERE app_name=@app", new { app }));
            Assert.NotNull(await db.ExecuteScalarAsync<DateTime?>("SELECT last_update FROM sw_app WHERE app_name=@app", new { app }));
        }
        finally
        {
            await host.DisposeAsync();
        }

        Assert.Equal("0", await db.ExecuteScalarAsync<string>("SELECT status FROM sw_app WHERE app_name=@app", new { app }));
        Assert.Equal("0", await db.ExecuteScalarAsync<string>("SELECT status FROM sw_nodes WHERE app_name=@app", new { app }));
        Assert.Equal((short)0, await db.ExecuteScalarAsync<short>("SELECT conn_out FROM sw_nodes WHERE app_name=@app", new { app }));
    }

    [Fact]
    public async Task Status_reporter_writes_connection_and_remote_columns()
    {
        Assert.SkipWhen(string.IsNullOrEmpty(ConnectionString), "Set SYNCNET_TEST_PG untuk menjalankan test PostgreSQL");
        (NpgsqlDataSource source, string app) = await SeedAsync(9201, 9202);
        await using NpgsqlDataSource _ = source;
        await using NpgsqlConnection db = await source.OpenConnectionAsync();
        string node = await db.ExecuteScalarAsync<string>("SELECT node_name FROM sw_nodes WHERE app_name=@app", new { app }) ?? "";
        var reporter = new PostgresNodeStatusReporter(source, TimeProvider.System, NullLogger<PostgresNodeStatusReporter>.Instance);

        await reporter.ReportRemoteNodeAsync(node, true);
        await reporter.ReportConnectionAsync(node + "_C1", true);
        await reporter.ReportCoreChannelAsync(node, CoreChannelDirection.Inbound, true);

        Assert.Equal((short)1, await db.ExecuteScalarAsync<short>("SELECT remote FROM sw_nodes WHERE node_name=@node", new { node }));
        Assert.NotNull(await db.ExecuteScalarAsync<DateTime?>("SELECT last_connected FROM sw_nodes WHERE node_name=@node", new { node }));
        Assert.Equal((short)1, await db.ExecuteScalarAsync<short>("SELECT remote FROM sw_connections WHERE conn_name=@c", new { c = node + "_C1" }));
        Assert.Equal((short)1, await db.ExecuteScalarAsync<short>("SELECT conn_in FROM sw_nodes WHERE node_name=@node", new { node }));

        await reporter.ReportConnectionAsync(node + "_C1", false);
        Assert.Equal((short)0, await db.ExecuteScalarAsync<short>("SELECT remote FROM sw_connections WHERE conn_name=@c", new { c = node + "_C1" }));
        Assert.NotNull(await db.ExecuteScalarAsync<DateTime?>("SELECT last_disconnected FROM sw_connections WHERE conn_name=@c", new { c = node + "_C1" }));
    }
}
