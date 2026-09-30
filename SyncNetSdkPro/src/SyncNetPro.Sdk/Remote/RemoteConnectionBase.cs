using System.Net;
using SyncNetPro.Sdk.Nodes;

namespace SyncNetPro.Sdk.Remote;

/// <summary>Callback dari koneksi ke manager.</summary>
internal interface IRemoteConnectionEvents
{
    Task OnConnectedAsync(RemoteConnectionBase connection, EndPoint? remote);

    Task OnDisconnectedAsync(RemoteConnectionBase connection, EndPoint? remote);

    /// <summary>Frame TCP yang tidak menyelesaikan request yang menunggu.</summary>
    Task OnTcpFrameAsync(RemoteConnectionBase connection, byte[] payload, EndPoint? remote, Func<ReadOnlyMemory<byte>, CancellationToken, Task> reply);

    /// <summary>Kunci korelasi dari pesan masuk (dari handler), atau null.</summary>
    string? GetCorrelationKey(RemoteConnectionInfo connection, byte[] payload);
}

/// <summary>Dasar implementasi koneksi remote.</summary>
internal abstract class RemoteConnectionBase(RemoteConnectionInfo info, NodeInfo node) : IRemoteConnection, IAsyncDisposable
{
    public RemoteConnectionInfo Info { get; } = info;

    public NodeInfo Node { get; set; } = node;

    public abstract bool IsConnected { get; }

    public abstract string? RemoteAddress { get; }

    /// <summary>Koneksi yang statusnya dilaporkan saat connect/disconnect (TCP persistent).</summary>
    public virtual bool ReportsSocketStatus => false;

    public abstract Task StartAsync(CancellationToken cancellationToken);

    public abstract ValueTask DisposeAsync();

    protected TimeSpan DefaultTimeout => TimeSpan.FromSeconds(Node.RequestTimeoutSeconds > 0 ? Node.RequestTimeoutSeconds : 30);
}
