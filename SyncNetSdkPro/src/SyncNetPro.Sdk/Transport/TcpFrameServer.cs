using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace SyncNetPro.Sdk.Transport;

/// <summary>Opsi <see cref="TcpFrameServer"/>.</summary>
public sealed record TcpFrameServerOptions
{
    /// <summary>Batas koneksi bersamaan (0 = tanpa batas).</summary>
    public int MaxConnections { get; init; }

    /// <summary>Hanya satu koneksi aktif; koneksi baru menggantikan yang lama.</summary>
    public bool SingleConnection { get; init; }

    /// <summary>Batas waktu menunggu sisa frame yang terpotong.</summary>
    public TimeSpan PartialFrameTimeout { get; init; } = TimeSpan.FromSeconds(10);
}

/// <summary>Server TCP dengan framing; setiap klien dilayani di task terpisah.</summary>
public sealed class TcpFrameServer : IAsyncDisposable
{
    private readonly IPEndPoint _bind;
    private readonly ITcpFrameCodec _codec;
    private readonly TcpFrameServerOptions _options;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<EndPoint, FramedConnection> _peers = new();
    private Socket? _listener;
    private IPEndPoint? _localEndPoint;
    private CancellationTokenSource? _cts;
    private Task? _acceptLoop;

    /// <summary>Membuat server (belum mendengarkan; panggil <see cref="StartAsync"/>).</summary>
    public TcpFrameServer(IPEndPoint bind, ITcpFrameCodec codec, TcpFrameServerOptions options, ILogger logger)
    {
        _bind = bind ?? throw new ArgumentNullException(nameof(bind));
        _codec = codec ?? throw new ArgumentNullException(nameof(codec));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Dipanggil setiap frame diterima dari sebuah koneksi.</summary>
    public Func<FramedConnection, byte[], Task>? FrameReceived { get; set; }

    /// <summary>Dipanggil saat klien terkoneksi.</summary>
    public Func<FramedConnection, Task>? Connected { get; set; }

    /// <summary>Dipanggil saat klien terputus.</summary>
    public Func<FramedConnection, Task>? Disconnected { get; set; }

    /// <summary>Alamat yang didengarkan (berguna bila port 0).</summary>
    public IPEndPoint LocalEndPoint => _localEndPoint ?? _bind;

    /// <summary>Koneksi aktif.</summary>
    public IReadOnlyCollection<FramedConnection> Connections => [.. _peers.Values];

    /// <summary>Ada minimal satu koneksi aktif.</summary>
    public bool HasConnections => _peers.Values.Any(p => p.IsOpen);

    /// <summary>Mulai mendengarkan.</summary>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_listener is not null) throw new InvalidOperationException("Server sudah berjalan.");

        var listener = new Socket(_bind.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        ConfigureAddressReuse(listener);
        try
        {
            listener.Bind(_bind);
        }
        catch
        {
            listener.Dispose();
            throw;
        }

        listener.Listen(512);
        _localEndPoint = (IPEndPoint)listener.LocalEndPoint!;
        _listener = listener;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _acceptLoop = Task.Run(() => AcceptLoopAsync(_cts.Token), CancellationToken.None);
        _logger.LogDebug("Mendengarkan di {EndPoint}", LocalEndPoint);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Port boleh dipakai ulang segera setelah restart (koneksi lama TIME_WAIT), tetapi dua listener aktif
    /// tidak boleh berbagi port. <c>SocketOptionName.ReuseAddress</c> di Linux/macOS juga memasang
    /// <c>SO_REUSEPORT</c> sehingga listener kedua diam-diam ikut menerima koneksi — karena itu hanya
    /// <c>SO_REUSEADDR</c> yang dipasang (sama dengan Kestrel), dan di Windows port dikunci eksklusif.
    /// </summary>
    internal static void ConfigureAddressReuse(Socket listener)
    {
        if (OperatingSystem.IsWindows())
        {
            listener.ExclusiveAddressUse = true;
        }
        else if (OperatingSystem.IsLinux())
        {
            listener.SetRawSocketOption(1 /* SOL_SOCKET */, 2 /* SO_REUSEADDR */, BitConverter.GetBytes(1));
        }
        else if (OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
        {
            listener.SetRawSocketOption(0xFFFF /* SOL_SOCKET */, 0x0004 /* SO_REUSEADDR */, BitConverter.GetBytes(1));
        }
    }

    /// <summary>Mengirim ke koneksi tertentu.</summary>
    /// <exception cref="NotConnectedException">Koneksi tidak ditemukan/tertutup.</exception>
    public Task SendToAsync(EndPoint remote, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default) =>
        _peers.TryGetValue(remote, out FramedConnection? peer) && peer.IsOpen
            ? peer.SendAsync(payload, cancellationToken)
            : throw new NotConnectedException($"Klien {remote} tidak terkoneksi.");

    /// <summary>Berhenti mendengarkan dan menutup semua koneksi.</summary>
    public async Task StopAsync()
    {
        if (_cts is null) return;
        await _cts.CancelAsync().ConfigureAwait(false);
        _listener?.Dispose();

        foreach (FramedConnection peer in _peers.Values) await peer.DisposeAsync().ConfigureAwait(false);

        if (_acceptLoop is not null) await _acceptLoop.ConfigureAwait(false);

        _peers.Clear();
        _cts.Dispose();
        _cts = null;
        _listener = null;
        _localEndPoint = null;
        _acceptLoop = null;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            Socket socket;
            try
            {
                socket = await _listener!.AcceptAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
            {
                break;
            }
            catch (SocketException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (SocketException ex)
            {
                _logger.LogWarning("Accept gagal di {EndPoint}: {Error}", LocalEndPoint, ex.Message);
                continue;
            }

            if (_options.SingleConnection)
            {
                foreach (FramedConnection old in _peers.Values) await old.DisposeAsync().ConfigureAwait(false);
            }
            else if (_options.MaxConnections > 0 && _peers.Count >= _options.MaxConnections)
            {
                _logger.LogWarning("Batas koneksi ({Max}) tercapai, menolak {Remote}", _options.MaxConnections, socket.RemoteEndPoint);
                socket.Dispose();
                continue;
            }

            var connection = new FramedConnection(socket, _codec, _options.PartialFrameTimeout);
            _peers[connection.RemoteEndPoint] = connection;
            _ = Task.Run(() => ServeAsync(connection, ct), CancellationToken.None);
        }
    }

    private async Task ServeAsync(FramedConnection connection, CancellationToken ct)
    {
        await SafeAsync(Connected, connection).ConfigureAwait(false);
        try
        {
            await connection.ReceiveLoopAsync(
                async payload =>
                {
                    if (FrameReceived is null) return;
                    try
                    {
                        await FrameReceived(connection, payload).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error memproses pesan dari {Remote}", connection.RemoteEndPoint);
                    }
                },
                ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or SocketException or InvalidDataException or TimeoutException or ObjectDisposedException or OperationCanceledException)
        {
            if (!ct.IsCancellationRequested) _logger.LogWarning("Koneksi {Remote} bermasalah: {Error}", connection.RemoteEndPoint, ex.Message);
        }
        finally
        {
            _peers.TryRemove(new KeyValuePair<EndPoint, FramedConnection>(connection.RemoteEndPoint, connection));
            await connection.DisposeAsync().ConfigureAwait(false);
            await SafeAsync(Disconnected, connection).ConfigureAwait(false);
        }
    }

    private async Task SafeAsync(Func<FramedConnection, Task>? callback, FramedConnection connection)
    {
        if (callback is null) return;
        try
        {
            await callback(connection).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pada callback koneksi {Remote}", connection.RemoteEndPoint);
        }
    }
}
