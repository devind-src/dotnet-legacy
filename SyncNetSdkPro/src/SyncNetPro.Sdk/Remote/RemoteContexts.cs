using System.Net;
using Microsoft.Extensions.Logging;
using SyncNetPro.Contracts;
using SyncNetPro.Sdk.Core;
using SyncNetPro.Sdk.Nodes;
using SyncNetPro.Sdk.Tracing;

namespace SyncNetPro.Sdk.Remote;

/// <summary>Konteks node (timer echo/key exchange).</summary>
public sealed class NodeContext : SyncNetContext
{
    internal NodeContext(NodeInfo node, SyncNetServices services, ILogger logger)
        : base(node, services, logger)
    {
    }

    /// <summary>Node.</summary>
    public new NodeInfo Node => base.Node!;
}

/// <summary>Konteks peristiwa koneksi remote (terkoneksi, terputus, auto sign-on).</summary>
public sealed class RemoteConnectionContext : SyncNetContext
{
    internal RemoteConnectionContext(NodeInfo node, IRemoteConnection connection, EndPoint? remoteEndPoint, SyncNetServices services, ILogger logger)
        : base(node, services, logger)
    {
        Connection = connection;
        RemoteEndPoint = remoteEndPoint;
    }

    /// <summary>Node.</summary>
    public new NodeInfo Node => base.Node!;

    /// <summary>Koneksi terkait.</summary>
    public IRemoteConnection Connection { get; }

    /// <summary>Alamat lawan.</summary>
    public EndPoint? RemoteEndPoint { get; }
}

/// <summary>
/// Pesan TCP dari sistem eksternal yang bukan balasan <see cref="IRemoteTcpConnection.SendAndReceiveAsync"/>
/// (mis. request dari bank/EDC, 0800, balasan terlambat).
/// </summary>
public sealed class RemoteMessageContext : SyncNetContext
{
    private readonly Func<ReadOnlyMemory<byte>, CancellationToken, Task> _reply;

    internal RemoteMessageContext(NodeInfo node, IRemoteConnection connection, byte[] message, EndPoint? remoteEndPoint,
        Func<ReadOnlyMemory<byte>, CancellationToken, Task> reply, SyncNetServices services, ILogger logger)
        : base(node, services, logger)
    {
        Connection = connection;
        Message = message;
        RemoteEndPoint = remoteEndPoint;
        _reply = reply;
    }

    /// <summary>Node.</summary>
    public new NodeInfo Node => base.Node!;

    /// <summary>Koneksi asal pesan.</summary>
    public IRemoteConnection Connection { get; }

    /// <summary>Pesan (tanpa header TCP).</summary>
    public byte[] Message { get; }

    /// <summary>Alamat pengirim.</summary>
    public EndPoint? RemoteEndPoint { get; }

    /// <summary>Alamat pengirim sebagai teks (IP:port).</summary>
    public string RemoteAddress => RemoteEndPoint?.ToString() ?? string.Empty;

    /// <summary>Membalas ke pengirim pada socket yang sama.</summary>
    public Task ReplyAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default) => _reply(payload, cancellationToken);

    /// <summary>
    /// Meneruskan request ke Core dan menunggu respons; <c>connection_name</c> dan <c>ip_external</c> diisi otomatis
    /// (setara <c>SendToSource(node, conn, IPEndPoint, request)</c> SDK lama).
    /// </summary>
    public Task<CoreResponse> SendToCoreAsync(CoreRequest request, CancellationToken cancellationToken = default) =>
        Core.SendAsync(Node.Name, request, new CoreSendOptions
        {
            ConnectionName = Connection.Info.Name,
            RemoteAddress = (RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? RemoteAddress,
        }, cancellationToken);
}

/// <summary>Request HTTP masuk dari sistem eksternal (koneksi peran server, protokol web service).</summary>
public sealed class HttpRequestContext : SyncNetContext
{
    internal HttpRequestContext(NodeInfo node, IRemoteConnection connection, string method, string path, string query,
        IReadOnlyDictionary<string, string> headers, string body, string? contentType, string remoteAddress,
        SyncNetServices services, ILogger logger)
        : base(node, services, logger)
    {
        Connection = connection;
        Method = method;
        Path = path;
        Query = query;
        Headers = headers;
        Body = body;
        ContentType = contentType;
        RemoteAddress = remoteAddress;
    }

    /// <summary>Node.</summary>
    public new NodeInfo Node => base.Node!;

    /// <summary>Koneksi (server HTTP) penerima.</summary>
    public IRemoteConnection Connection { get; }

    /// <summary>Method HTTP.</summary>
    public string Method { get; }

    /// <summary>Path (mis. <c>/payment</c>).</summary>
    public string Path { get; }

    /// <summary>Query string (termasuk <c>?</c>) atau kosong.</summary>
    public string Query { get; }

    /// <summary>Header request (nama tidak peka huruf besar/kecil).</summary>
    public IReadOnlyDictionary<string, string> Headers { get; }

    /// <summary>Isi request (UTF-8).</summary>
    public string Body { get; }

    /// <summary>Content type request.</summary>
    public string? ContentType { get; }

    /// <summary>IP klien.</summary>
    public string RemoteAddress { get; }

    /// <summary>
    /// Meneruskan request ke Core dan menunggu respons; <c>connection_name</c> dan <c>ip_external</c> diisi otomatis
    /// (setara <c>SendToSource(node, conn, HttpContext, request)</c> SDK lama — tanpa cache <c>HttpContext</c>).
    /// </summary>
    public Task<CoreResponse> SendToCoreAsync(CoreRequest request, CancellationToken cancellationToken = default) =>
        Core.SendAsync(Node.Name, request, new CoreSendOptions { ConnectionName = Connection.Info.Name, RemoteAddress = RemoteAddress }, cancellationToken);
}
