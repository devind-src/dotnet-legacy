using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using SyncNetPro.Sdk.Nodes;
using SyncNetPro.Sdk.Transport;

namespace SyncNetPro.Sdk.Remote;

/// <summary>TCP klien persistent (<c>always_connected = 1</c>) dengan koneksi ulang otomatis.</summary>
internal sealed class PersistentTcpClientConnection : RemoteConnectionBase, IRemoteTcpConnection
{
    private readonly ITcpFrameCodec _codec;
    private readonly IRemoteConnectionEvents _events;
    private readonly ILogger _logger;
    private readonly TimeProvider _time;
    private readonly RemotePendingStore _pending = new();
    private readonly TimeSpan _connectTimeout;
    private TcpFrameClient _client;

    public PersistentTcpClientConnection(RemoteConnectionInfo info, NodeInfo node, ITcpFrameCodec codec, IRemoteConnectionEvents events,
        TimeSpan connectTimeout, ILogger logger, TimeProvider time)
        : base(info, node)
    {
        if (string.IsNullOrWhiteSpace(info.Host) || info.Port <= 0)
            throw new InvalidOperationException($"Koneksi {info.Name}: ip_address/port tujuan tidak valid ({info.Host}:{info.Port}).");

        _codec = codec;
        _events = events;
        _logger = logger;
        _time = time;
        _connectTimeout = connectTimeout;
        _client = CreateClient();
    }

    public override bool IsConnected => _client.IsConnected;

    public override string? RemoteAddress => $"{Info.Host}:{Info.Port}";

    public override bool ReportsSocketStatus => true;

    public override Task StartAsync(CancellationToken cancellationToken) => _client.StartAsync(cancellationToken);

    public async Task SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.SendAsync(payload, cancellationToken).ConfigureAwait(false);
        }
        catch (NotConnectedException ex)
        {
            throw new RemoteUnavailableException($"Koneksi {Info.Name} ({RemoteAddress}) tidak terhubung.", ex);
        }
    }

    public async Task<byte[]> SendAndReceiveAsync(ReadOnlyMemory<byte> payload, string correlationKey, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(correlationKey);
        TaskCompletionSource<byte[]> completion = _pending.Register(correlationKey, Info.Name);
        try
        {
            await SendAsync(payload, cancellationToken).ConfigureAwait(false);
            return await RemotePendingStore.AwaitAsync(completion, timeout ?? DefaultTimeout, _time, $"{Info.Name} ({RemoteAddress})", cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _pending.Remove(correlationKey, completion);
        }
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _client.DisposeAsync().ConfigureAwait(false);
        await Task.Delay(TimeSpan.FromSeconds(1), _time, cancellationToken).ConfigureAwait(false);
        _client = CreateClient();
        await _client.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public override async ValueTask DisposeAsync()
    {
        _pending.FailAll(new RemoteUnavailableException($"Koneksi {Info.Name} ditutup."));
        await _client.DisposeAsync().ConfigureAwait(false);
    }

    private TcpFrameClient CreateClient()
    {
        var client = new TcpFrameClient(Info.Host!, Info.Port, _codec, new TcpFrameClientOptions
        {
            ReconnectDelay = Info.RetryDelaySeconds > 0 ? TimeSpan.FromSeconds(Info.RetryDelaySeconds) : null,
            ConnectTimeout = _connectTimeout,
        }, _logger, _time);

        client.Connected = remote => _events.OnConnectedAsync(this, remote);
        client.Disconnected = remote => _events.OnDisconnectedAsync(this, remote);
        client.FrameReceived = payload =>
        {
            string? key = _events.GetCorrelationKey(Info, payload);
            if (key is not null && _pending.TryComplete(key, payload)) return Task.CompletedTask;
            return _events.OnTcpFrameAsync(this, payload, client.RemoteEndPoint, (reply, ct) => SendAsync(reply, ct));
        };
        return client;
    }
}

/// <summary>
/// TCP klien non-persistent (<c>always_connected = 0</c>): setiap pengiriman membuka koneksi baru, menunggu
/// balasan, lalu menutup. Memakai codec yang sama dengan mode persistent (perbaikan B1).
/// </summary>
internal sealed class NonPersistentTcpClientConnection : RemoteConnectionBase, IRemoteTcpConnection
{
    private readonly ITcpFrameCodec _codec;
    private readonly IRemoteConnectionEvents _events;
    private readonly ILogger _logger;
    private readonly TimeProvider _time;
    private readonly TimeSpan _connectTimeout;
    private readonly CancellationTokenSource _disposed = new();

    public NonPersistentTcpClientConnection(RemoteConnectionInfo info, NodeInfo node, ITcpFrameCodec codec, IRemoteConnectionEvents events,
        TimeSpan connectTimeout, ILogger logger, TimeProvider time)
        : base(info, node)
    {
        if (string.IsNullOrWhiteSpace(info.Host) || info.Port <= 0)
            throw new InvalidOperationException($"Koneksi {info.Name}: ip_address/port tujuan tidak valid ({info.Host}:{info.Port}).");

        _codec = codec;
        _events = events;
        _logger = logger;
        _time = time;
        _connectTimeout = connectTimeout;
    }

    public override bool IsConnected => true;

    public override string? RemoteAddress => $"{Info.Host}:{Info.Port}";

    public override Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        FramedConnection connection = await ConnectAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await connection.SendAsync(payload, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        // Balasan (bila ada) diteruskan ke OnRemoteMessageAsync sampai timeout/ditutup lawan.
        _ = Task.Run(async () =>
        {
            await using (connection.ConfigureAwait(false))
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(_disposed.Token);
                cts.CancelAfter(DefaultTimeout);
                try
                {
                    await connection.ReceiveLoopAsync(
                        frame => _events.OnTcpFrameAsync(this, frame, connection.RemoteEndPoint, (reply, ct) => connection.SendAsync(reply, ct)),
                        cts.Token).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is OperationCanceledException or IOException or SocketException or InvalidDataException or TimeoutException or ObjectDisposedException)
                {
                    // selesai: timeout / ditutup
                }
            }
        }, CancellationToken.None);
    }

    public async Task<byte[]> SendAndReceiveAsync(ReadOnlyMemory<byte> payload, string correlationKey, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        TimeSpan wait = timeout ?? DefaultTimeout;
        await using FramedConnection connection = await ConnectAsync(cancellationToken).ConfigureAwait(false);
        var first = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposed.Token);

        Task loop = Task.Run(async () =>
        {
            try
            {
                await connection.ReceiveLoopAsync(
                    frame =>
                    {
                        if (!first.TrySetResult(frame))
                        {
                            return _events.OnTcpFrameAsync(this, frame, connection.RemoteEndPoint, (reply, ct) => connection.SendAsync(reply, ct));
                        }

                        return Task.CompletedTask;
                    },
                    cts.Token).ConfigureAwait(false);
                first.TrySetException(new RemoteUnavailableException($"Koneksi {Info.Name} ({RemoteAddress}) ditutup sebelum ada balasan."));
            }
            catch (Exception ex) when (ex is OperationCanceledException or IOException or SocketException or InvalidDataException or TimeoutException or ObjectDisposedException)
            {
                first.TrySetException(new RemoteUnavailableException($"Koneksi {Info.Name} ({RemoteAddress}) gagal: {ex.Message}", ex));
            }
        }, CancellationToken.None);

        try
        {
            await connection.SendAsync(payload, cancellationToken).ConfigureAwait(false);
            return await RemotePendingStore.AwaitAsync(first, wait, _time, $"{Info.Name} ({RemoteAddress})", cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await cts.CancelAsync().ConfigureAwait(false);
            await connection.DisposeAsync().ConfigureAwait(false);
            await loop.ConfigureAwait(false);
        }
    }

    public Task ResetAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public override async ValueTask DisposeAsync()
    {
        await _disposed.CancelAsync().ConfigureAwait(false);
        _disposed.Dispose();
    }

    private async Task<FramedConnection> ConnectAsync(CancellationToken cancellationToken)
    {
        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposed.Token);
        cts.CancelAfter(_connectTimeout);
        try
        {
            await socket.ConnectAsync(Info.Host!, Info.Port, cts.Token).ConfigureAwait(false);
            return new FramedConnection(socket, _codec, TimeSpan.FromSeconds(10));
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            socket.Dispose();
            _logger.LogWarning("Gagal koneksi ke {Target} ({Connection}): {Error}", RemoteAddress, Info.Name, ex.Message);
            throw new RemoteUnavailableException($"Tidak dapat terhubung ke {Info.Name} ({RemoteAddress}).", ex);
        }
    }
}

/// <summary>TCP server (<c>conn_type = 0</c>): sistem eksternal terhubung ke interface.</summary>
internal sealed class TcpServerConnection : RemoteConnectionBase, IRemoteTcpConnection
{
    private readonly ITcpFrameCodec _codec;
    private readonly IRemoteConnectionEvents _events;
    private readonly ILogger _logger;
    private readonly TimeProvider _time;
    private readonly RemotePendingStore _pending = new();
    private TcpFrameServer _server;

    public TcpServerConnection(RemoteConnectionInfo info, NodeInfo node, ITcpFrameCodec codec, IRemoteConnectionEvents events, ILogger logger, TimeProvider time)
        : base(info, node)
    {
        if (info.Port < 0 || info.Port > 65535) throw new InvalidOperationException($"Koneksi {info.Name}: port {info.Port} tidak valid.");
        _codec = codec;
        _events = events;
        _logger = logger;
        _time = time;
        _server = CreateServer();
    }

    public override bool IsConnected => !Info.AlwaysConnected || _server.HasConnections;

    public override string? RemoteAddress => _server.Connections.FirstOrDefault(c => c.IsOpen)?.RemoteEndPoint.ToString();

    public override bool ReportsSocketStatus => Info.AlwaysConnected;

    /// <summary>Alamat yang didengarkan (untuk test/port 0).</summary>
    public IPEndPoint LocalEndPoint => _server.LocalEndPoint;

    public override Task StartAsync(CancellationToken cancellationToken) => _server.StartAsync(cancellationToken);

    public Task SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        FramedConnection connection = _server.Connections.FirstOrDefault(c => c.IsOpen)
            ?? throw new RemoteUnavailableException($"Belum ada klien terhubung ke {Info.Name} (port {Info.Port}).");
        return connection.SendAsync(payload, cancellationToken);
    }

    public async Task<byte[]> SendAndReceiveAsync(ReadOnlyMemory<byte> payload, string correlationKey, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(correlationKey);
        TaskCompletionSource<byte[]> completion = _pending.Register(correlationKey, Info.Name);
        try
        {
            await SendAsync(payload, cancellationToken).ConfigureAwait(false);
            return await RemotePendingStore.AwaitAsync(completion, timeout ?? DefaultTimeout, _time, $"{Info.Name} ({RemoteAddress})", cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _pending.Remove(correlationKey, completion);
        }
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _server.DisposeAsync().ConfigureAwait(false);
        await Task.Delay(TimeSpan.FromSeconds(1), _time, cancellationToken).ConfigureAwait(false);
        _server = CreateServer();
        await _server.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public override async ValueTask DisposeAsync()
    {
        _pending.FailAll(new RemoteUnavailableException($"Koneksi {Info.Name} ditutup."));
        await _server.DisposeAsync().ConfigureAwait(false);
    }

    private TcpFrameServer CreateServer()
    {
        IPAddress address = string.IsNullOrWhiteSpace(Info.Host) || Info.Host is "0.0.0.0" or "*" ? IPAddress.Any
            : IPAddress.TryParse(Info.Host, out IPAddress? parsed) ? parsed
            : throw new InvalidOperationException($"Koneksi {Info.Name}: alamat bind tidak valid ({Info.Host}).");

        var server = new TcpFrameServer(new IPEndPoint(address, Info.Port), _codec, new TcpFrameServerOptions
        {
            MaxConnections = Info.MaxConnections,
            SingleConnection = Info.OneSocketOnly,
        }, _logger);

        server.Connected = c => _events.OnConnectedAsync(this, c.RemoteEndPoint);
        server.Disconnected = c => _events.OnDisconnectedAsync(this, c.RemoteEndPoint);
        server.FrameReceived = (c, payload) =>
        {
            string? key = _events.GetCorrelationKey(Info, payload);
            if (key is not null && _pending.TryComplete(key, payload)) return Task.CompletedTask;
            return _events.OnTcpFrameAsync(this, payload, c.RemoteEndPoint, (reply, ct) => c.SendAsync(reply, ct));
        };
        return server;
    }
}
