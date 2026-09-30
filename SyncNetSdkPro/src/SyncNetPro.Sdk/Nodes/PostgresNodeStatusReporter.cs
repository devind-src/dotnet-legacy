using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace SyncNetPro.Sdk.Nodes;

/// <summary>
/// Menulis status dengan SQL yang sama dengan <c>DbMgr.Update*</c> SDK lama (nilai 0/1, waktu lokal).
/// Kegagalan dicatat, tidak dilempar (status bukan jalur transaksi).
/// </summary>
public sealed class PostgresNodeStatusReporter(NpgsqlDataSource dataSource, TimeProvider time, ILogger<PostgresNodeStatusReporter> logger) : INodeStatusReporter
{
    /// <inheritdoc />
    public async Task ReportApplicationAsync(string appName, bool up, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync("UPDATE sw_app SET status = @status, last_update = @last_update WHERE app_name = @app_name",
            new { status = Status(up), last_update = Now(), app_name = appName }, cancellationToken).ConfigureAwait(false);
        await ExecuteAsync("UPDATE sw_nodes SET status = @status WHERE app_name = @app_name",
            new { status = Status(up), app_name = appName }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task ReportCoreChannelAsync(string nodeName, CoreChannelDirection direction, bool up, CancellationToken cancellationToken = default)
    {
        string column = direction == CoreChannelDirection.Inbound ? "conn_in" : "conn_out";
        return ExecuteAsync($"UPDATE sw_nodes SET {column} = @status WHERE node_name = @node_name",
            new { status = Status(up), node_name = nodeName }, cancellationToken);
    }

    /// <inheritdoc />
    public Task ReportRemoteNodeAsync(string nodeName, bool up, CancellationToken cancellationToken = default)
    {
        string column = up ? "last_connected" : "last_disconnected";
        return ExecuteAsync($"UPDATE sw_nodes SET {column} = @dtnow, remote = @status WHERE node_name = @node_name",
            new { status = Status(up), dtnow = Now(), node_name = nodeName }, cancellationToken);
    }

    /// <inheritdoc />
    public Task ReportConnectionAsync(string connectionName, bool up, CancellationToken cancellationToken = default)
    {
        string column = up ? "last_connected" : "last_disconnected";
        return ExecuteAsync($"UPDATE sw_connections SET {column} = @dtnow, remote = @status WHERE conn_name = @conn_name",
            new { status = Status(up), dtnow = Now(), conn_name = connectionName }, cancellationToken);
    }

    private static int Status(bool up) => up ? 1 : 0;

    // Kolom tanpa zona waktu; SDK lama menulis DateTime.Now (waktu lokal server).
    private DateTime Now() => DateTime.SpecifyKind(time.GetLocalNow().DateTime, DateTimeKind.Unspecified);

    private async Task ExecuteAsync(string sql, object args, CancellationToken cancellationToken)
    {
        try
        {
            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await connection.ExecuteAsync(new CommandDefinition(sql, args, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is NpgsqlException or InvalidOperationException or TimeoutException)
        {
            logger.LogWarning("Gagal memperbarui status: {Error} ({Sql})", ex.Message, sql);
        }
    }
}
