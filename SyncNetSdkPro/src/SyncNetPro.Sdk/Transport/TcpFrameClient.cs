using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace SyncNetPro.Sdk.Transport;

/// <summary>Opsi <see cref="TcpFrameClient"/>.</summary>
public sealed record TcpFrameClientOptions
{
    /// <summary>Jeda tetap sebelum mencoba koneksi ulang. <c>null</c> = backoff eksponensial 1–30 detik.</summary>
    public TimeSpan? ReconnectDelay { get; init; }

    /// <summary>Batas waktu handshake TCP.</summary>
    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>Batas waktu menunggu sisa frame yang terpotong.</summary>
    public TimeSpan PartialFrameTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>Koneksi ulang otomatis setelah terputus.</summary>
    public bool AutoReconnect { get; init; } = true;
}

/// <summary>
/// Klien TCP persistent dengan framing dan koneksi ulang otomatis. Semua error dicatat ke
/// <see cref="ILogger"/> dan status koneksi dapat dibaca (<see cref="IsConnected"/>) — tidak ada
/// exception yang hilang di background (perbaikan B8).
/// </summary>
public sealed class TcpFrameClient : IAsyncDisposable
{
    private readonly string _host;
    private readonly int _port;
    private readonly ITcpFrameCodec _codec;
    private readonly TcpFrameClientOptions _options;
    private readonly ILogger _logger;
    private readonly TimeProvider _time;
    private CancellationTokenSource? _cts;
    private Task? _loop;
    private FramedConnection? _connection;

    /// <summary>Membuat klien (belum terkoneksi; panggil <see cref="StartAsync"/>).</summary>
    public TcpFrameClient(string host, int port, ITcpFrameCodec codec, TcpFrameClientOptions options, ILogger logger, TimeProvider? timeProvider = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(port);
        _host = host;
        _port = port;
        _codec = codec ?? throw new ArgumentNullException(nameof(codec));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _time = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Dipanggil setiap frame diterima (berurutan per koneksi).</summary>
    public Func<byte[], Task>? FrameReceived { get; set; }

    /// <summary>Dipanggil setelah koneksi terbentuk.</summary>
    public Func<EndPoint, Task>? Connected { get; set; }

    /// <summary>Dipanggil setelah koneksi terputus.</summary>
    public Func<EndPoint, Task>? Disconnected { get; set; }

    /// <summary>Tujuan koneksi dalam format <c>host:port</c>.</summary>
    public string Target => $"{_host}:{_port}";

    /// <summary>Status koneksi saat ini.</summary>
    public bool IsConnected => _connection?.IsOpen == true;

    /// <summary>Alamat lawan saat terkoneksi.</summary>
    public EndPoint? RemoteEndPoint => _connection?.RemoteEndPoint;

    /// <summary>Memulai loop koneksi di background.</summary>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_loop is not null) throw new InvalidOperationException("Klien sudah dijalankan.");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loop = Task.Run(() => RunAsync(_cts.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    /// <summary>Mengirim payload.</summary>
    /// <exception cref="NotConnectedException">Belum/tidak terkoneksi.</exception>
    public async Task SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        FramedConnection connection = _connection is { IsOpen: true } c
            ? c
            : throw new NotConnectedException($"Tidak terkoneksi ke {Target}.");

        try
        {
            await connection.SendAsync(payload, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException)
        {
            throw new NotConnectedException($"Gagal mengirim ke {Target}: {ex.Message}", ex);
        }
    }

    /// <summary>Menghentikan loop dan menutup koneksi.</summary>
    public async Task StopAsync()
    {
        if (_cts is null) return;
        await _cts.CancelAsync().ConfigureAwait(false);

        FramedConnection? connection = _connection;
        if (connection is not null) await connection.DisposeAsync().ConfigureAwait(false);

        if (_loop is not null)
        {
            try
            {
                await _loop.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        _cts.Dispose();
        _cts = null;
        _loop = null;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);

    private async Task RunAsync(CancellationToken ct)
    {
        int attempt = 0;
        while (!ct.IsCancellationRequested)
        {
            Socket? socket = null;
            try
            {
                socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                using (var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
                {
                    connectCts.CancelAfter(_options.ConnectTimeout);
                    try
                    {
                        await socket.ConnectAsync(_host, _port, connectCts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                    {
                        throw new TimeoutException($"Timeout koneksi ke {Target}.");
                    }
                }

                var connection = new FramedConnection(socket, _codec, _options.PartialFrameTimeout);
                socket = null;
                _connection = connection;
                attempt = 0;
                EndPoint remote = connection.RemoteEndPoint;

                _logger.LogInformation("Terkoneksi ke {Target} ({Remote})", Target, remote);
                await InvokeAsync(Connected, remote).ConfigureAwait(false);

                try
                {
                    await connection.ReceiveLoopAsync(OnFrameAsync, ct).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is IOException or SocketException or InvalidDataException or TimeoutException or ObjectDisposedException)
                {
                    if (!ct.IsCancellationRequested) _logger.LogWarning("Koneksi ke {Target} bermasalah: {Error}", Target, ex.Message);
                }
                finally
                {
                    _connection = null;
                    await connection.DisposeAsync().ConfigureAwait(false);
                    _logger.LogInformation("Terputus dari {Target}", Target);
                    await InvokeAsync(Disconnected, remote).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (ex is SocketException or TimeoutException or IOException)
            {
                // Throttle: log tiap percobaan pertama lalu setiap 12 kali (≈ 1 menit pada jeda 5 detik).
                if (attempt % 12 == 0) _logger.LogWarning("Gagal koneksi ke {Target}: {Error}", Target, ex.Message);
            }
            finally
            {
                socket?.Dispose();
            }

            if (!_options.AutoReconnect || ct.IsCancellationRequested) break;

            try
            {
                await Task.Delay(NextDelay(attempt++), _time, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private TimeSpan NextDelay(int attempt)
    {
        if (_options.ReconnectDelay is { } fixedDelay) return fixedDelay;
        double seconds = Math.Min(30, Math.Pow(2, Math.Min(attempt, 5)));
        return TimeSpan.FromSeconds(seconds * (0.8 + (Random.Shared.NextDouble() * 0.4)));
    }

    private async Task OnFrameAsync(byte[] payload)
    {
        if (FrameReceived is null) return;
        try
        {
            await FrameReceived(payload).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error memproses pesan dari {Target}", Target);
        }
    }

    private async Task InvokeAsync(Func<EndPoint, Task>? callback, EndPoint endPoint)
    {
        if (callback is null) return;
        try
        {
            await callback(endPoint).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pada callback koneksi {Target}", Target);
        }
    }
}
