using SyncNet.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SyncNet.Networking
{
    public class XTcpClient
    {
        private const int MaxPacketSize = 65535; // Maksimal ukuran paket (header + payload)

        // Events
        public event Func<byte[], EndPoint, Task> OnDataArrival;
        public event Func<Exception, Task> OnError;
        public event Func<EndPoint, Task> OnConnect;
        public event Func<EndPoint, Task> OnDisconnect;
        public event Func<string, Task> OnStopped;

        // Property jika class digunakan independen
        public TcpHeaderType DefaultHeaderType { get; set; } = TcpHeaderType.Binary2Byte;
        public TcpLengthMode DefaultLengthMode { get; set; } = TcpLengthMode.Exclude;
        public TcpEndianMode DefaultEndianMode { get; set; } = TcpEndianMode.BigEndian;

        public bool AutoReconnect { get; set; } = true;
        public int RetryDelaySeconds { get; set; } = 0;
        public int ReadTimeoutMs { get; set; } = 10000; // 10 detik default timeout

        // Private variables
        private string _host;
        private int _port;

        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;
        private EndPoint _activeRemoteEndPoint;

        private readonly RetryPolicyDelay _retryPolicy = new();
        private readonly SemaphoreSlim _sendLock = new(1, 1);

        public XTcpClient() { }
        public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
        {
            _host = host;
            _port = port;

            _cts?.Cancel();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Jalankan loop di background
            _ = Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        _client = new TcpClient();

                        // Batas waktu maksimal untuk handshake koneksi TCP
                        TimeSpan handshakeTimeout = TimeSpan.FromSeconds(5);
                        using var timeoutCts = new CancellationTokenSource(handshakeTimeout);
                        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, timeoutCts.Token);

                        // Coba koneksi dengan timeout
                        await _client.ConnectAsync(_host, _port, linkedCts.Token);

                        _stream = _client.GetStream();
                        _activeRemoteEndPoint = _client.Client.RemoteEndPoint!;

                        RaiseOnConnect(_activeRemoteEndPoint);

                        // Mulai loop penerimaan data di background
                        await ReceiveLoopAsync(_cts.Token);
                    }
                    catch (OperationCanceledException) when (_cts.Token.IsCancellationRequested)
                    {
                        // Jika pembatalan dipicu secara sengaja (misal via DisconnectAsync), keluar dari loop master
                        break;
                    }
                    // Tambahkan catch khusus untuk menangani Timeout
                    catch (Exception ex) when (ex is OperationCanceledException || ex is TaskCanceledException)
                    {
                        // Jika token utama (_cts) yang membatalkan, langsung keluar
                        if (_cts == null || _cts.Token.IsCancellationRequested) break;

                        // Jika karena custom timeout, buat exception baru dan jalankan jeda retry
                        RaiseOnError(new TimeoutException($"Retry connect to {_host}:{_port} failed.", ex));

                        if (AutoReconnect == false) throw;
                        await DelayBeforeRetry(); // Properti RetryDelaySeconds akan berjalan di sini!
                    }
                    catch (SocketException ex)
                    {
                        // khusus error koneksi ditolak
                        if (ex.SocketErrorCode == SocketError.ConnectionRefused)
                        {
                            RaiseOnError(new Exception($"Connection to {_host},{_port} refused", ex));
                        }
                        else
                        {
                            RaiseOnError(ex);
                        }
                    }
                    catch (Exception ex)
                    {
                        RaiseOnError(ex);
                    }

                    // Cek kondisi AutoReconnect di master loop
                    if (!AutoReconnect || _cts.Token.IsCancellationRequested)
                    {
                        break; // Jika AutoReconnect dinonaktifkan atau aplikasi di-stop, hentikan master loop
                    }

                    // Jeda retry dieksekusi di master loop setelah ReceiveLoopAsync selesai/terputus
                    try
                    {
                        await DelayBeforeRetry();
                    }
                    catch (OperationCanceledException)
                    {
                        break; // Keluar jika token dibatalkan saat jeda delay
                    }
                }
            }, _cts.Token);

            await Task.CompletedTask;
        }
        public async Task DisconnectAsync()
        {
            try
            {
                _cts?.Cancel(); // Hentikan loop background

                if (_client != null && _client.Connected)
                {
                    // Tutup stream
                    _stream?.Close();
                    _stream?.Dispose();

                    // Tutup client
                    _client?.Close();
                    _client?.Dispose();

                    RaiseOnStopped("Service closed manually");
                }
            }
            catch (Exception ex)
            {
                RaiseOnError(ex);
            }

            await Task.CompletedTask;
        }
        private async Task DelayBeforeRetry()
        {
            if (RetryDelaySeconds == 0)
            {
                // Jeda waktu sebelum mencoba re-koneksi kembali (menghindari spamming loop)
                var delaySec = _retryPolicy.GetCurrentDelaySeconds();
                await Task.Delay(delaySec, _cts.Token);
                _retryPolicy.Increment();
            }
            else
            {
                // re-koneksi setiap x detik
                var delaySec = TimeSpan.FromSeconds(RetryDelaySeconds);
                await Task.Delay(delaySec, _cts.Token);
            }
        }

        public async Task SendAsync(string payload, CancellationToken cancellationToken = default)
        {
            await SendAsync(payload.StringToBytes(), cancellationToken);
        }
        public async Task SendAsync(byte[] payload, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_stream == null) throw new InvalidOperationException("Data transmission failed, connection not established.");

                // Tambahkan header sesuai format yang digunakan
                await SendPacketAsync(_stream, payload, cancellationToken);
            }
            catch (Exception ex)
            {
                RaiseOnError(ex);
            }
        }

        private async Task SendPacketAsync(NetworkStream stream, byte[] payload, CancellationToken cancellationToken)
        {
            await _sendLock.WaitAsync(cancellationToken);

            try
            {
                // Tambahkan header sesuai format yang digunakan
                byte[] packet = TcpHeader.AddTcpHeader(payload,
                    DefaultHeaderType,
                    DefaultLengthMode,
                    DefaultEndianMode);

                await stream.WriteAsync(packet, cancellationToken);
            }
            finally
            {
                _sendLock.Release();
            }
        }
        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (_client != null && _client.Connected && !cancellationToken.IsCancellationRequested)
                {
                    byte[] reply = await ReceiveReplyAsync(_stream!, cancellationToken);

                    // Jika ReceiveReplyAsync mengembalikan null, artinya server menutup koneksi secara anggun (graceful)
                    if (reply == null) break;

                    RaiseOnDataArrival(reply, _activeRemoteEndPoint);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                RaiseOnError(ex);
            }
            finally
            {
                // Trigger event disconnect
                if (_activeRemoteEndPoint != null)
                {
                    RaiseOnDisconnect(_activeRemoteEndPoint);
                }

                // Clean resources
                _stream?.Close();
                _stream?.Dispose();
                _stream = null;

                _client?.Close();
                _client?.Dispose();
                _client = null;
            }
        }
        private async Task<byte[]> ReceiveReplyAsync(NetworkStream stream, CancellationToken cancellationToken)
        {
            // Cek header length sesuai format yang digunakan
            int lengthHeader = TcpHeader.GetLengthHeader(DefaultHeaderType);

            // Baca header
            byte[] header = new byte[lengthHeader];

            try
            {
                // Menunggu sampai 'lengthHeader' terpenuhi penuh.
                await stream.ReadExactlyAsync(header, cancellationToken);
            }
            catch (EndOfStreamException)
            {
                return null; // Koneksi ditutup oleh remote
            }

            // Ambil panjang payload dari header
            int length = TcpHeader.GetLengthMessage(header,
                DefaultHeaderType, DefaultLengthMode, DefaultEndianMode);

            // Validasi panjang payload
            if (length <= 0 || length > MaxPacketSize)
            {
                RaiseOnError(new InvalidOperationException($"Invalid packet length {length}"));
                return null; // discard paket rusak
            }

            // Baca payload sesuai length
            byte[] buffer = new byte[length];

            using (var payloadCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                payloadCts.CancelAfter(ReadTimeoutMs);

                try
                {
                    // Menunggu sampai seluruh payload (panjang 'length') selesai diunduh secara utuh
                    await stream.ReadExactlyAsync(buffer, payloadCts.Token);
                }
                catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    RaiseOnError(new TimeoutException($"Read payload timeout ({ReadTimeoutMs}ms) exceeded."));
                    return null;
                }
                catch (EndOfStreamException)
                {
                    RaiseOnError(new InvalidDataException("Incomplete payload received (Stream closed prematurely)"));
                    return null; // discard paket rusak
                }
            }

            // Valid
            return buffer;
        }

        public bool IsConnected()
        {
            try
            {
                if (_client == null || _client.Client == null)
                    return false;

                // Pastikan socket masih terhubung
                return _client.Connected && !(_client.Client.Poll(1, SelectMode.SelectRead) && _client.Client.Available == 0);
            }
            catch
            {
                return false;
            }
        }

        #region Non Persistent Methods Tanpa Event
        public async Task<byte[]> ConnectAndSendAsync(string host, int port, string payload, CancellationToken cancellationToken = default)
        {
            return await ConnectAndSendAsync(host, port, payload.StringToBytes(), cancellationToken);
        }
        public async Task<byte[]> ConnectAndSendAsync(string host, int port, byte[] payload, CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = new TcpClient();
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                await client.ConnectAsync(host, port, cancellationToken);
                using var stream = client.GetStream();

                // Tambahkan header sesuai format yang digunakan
                await SendPacketAsync(stream, payload, cancellationToken);

                // baca reply
                byte[] reply = await ReceiveReplyAsync(stream, cancellationToken);

                client.Close();

                return reply;
            }
            catch (Exception ex)
            {
                RaiseOnError(ex);
                return null;
            }
        }
        #endregion

        // === MULTICAST FIRE-AND-FORGET HELPER ===
        // === METODE PEMICU KHUSUS MASING-MASING EVENT (AMBIL & JALANKAN) ===
        private void RaiseOnConnect(EndPoint remoteEndPoint)
        {
            // reset value delay
            _retryPolicy.Reset();

            var handler = OnConnect;
            if (handler == null) return;

            // Membongkar multicast list khusus untuk Func<EndPoint, Task>
            foreach (Func<EndPoint, Task> singleCast in handler.GetInvocationList().Cast<Func<EndPoint, Task>>())
            {
                _ = Task.Run(async () =>
                {
                    try { await singleCast(remoteEndPoint); }
                    catch { /* Proteksi agar internal socket tidak crash jika kode luar error */ }
                });
            }
        }
        private void RaiseOnDisconnect(EndPoint remoteEndPoint)
        {
            var handler = OnDisconnect;
            if (handler == null) return;

            // Membongkar multicast list khusus untuk Func<EndPoint, Task>
            foreach (Func<EndPoint, Task> singleCast in handler.GetInvocationList().Cast<Func<EndPoint, Task>>())
            {
                _ = Task.Run(async () =>
                {
                    try { await singleCast(remoteEndPoint); }
                    catch { }
                });
            }
        }
        private void RaiseOnError(Exception exception)
        {
            var handler = OnError;
            if (handler == null) return;

            // Membongkar multicast list khusus untuk Func<Exception, Task>
            foreach (Func<Exception, Task> singleCast in handler.GetInvocationList().Cast<Func<Exception, Task>>())
            {
                _ = Task.Run(async () =>
                {
                    try { await singleCast(exception); }
                    catch { }
                });
            }
        }
        private void RaiseOnStopped(string message)
        {
            var handler = OnStopped;
            if (handler == null) return;

            foreach (Delegate singleCast in handler.GetInvocationList())
            {
                if (singleCast is Func<string, Task> func)
                {
                    _ = Task.Run(async () =>
                    {
                        try { await func(message); }
                        catch { }
                    });
                }
            }
        }
        private void RaiseOnDataArrival(byte[] data, EndPoint remoteEndPoint)
        {
            var handler = OnDataArrival;
            if (handler == null) return;

            // Membongkar multicast list khusus untuk Func<byte[], EndPoint, Task>
            foreach (Func<byte[], EndPoint, Task> singleCast in handler.GetInvocationList().Cast<Func<byte[], EndPoint, Task>>())
            {
                _ = Task.Run(async () =>
                {
                    try { await singleCast(data, remoteEndPoint); }
                    catch { }
                });
            }
        }
    }
}
