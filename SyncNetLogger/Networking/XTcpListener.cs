using SyncNet.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SyncNet.Networking
{
    public class XTcpListener
    {
        const int MaxPacketSize = 65535; // Maksimal ukuran paket (header + payload)

        // Events
        public event Func<byte[], EndPoint, Task> OnDataArrival;
        public event Func<EndPoint, Task> OnConnect;
        public event Func<EndPoint, Task> OnDisconnect;
        public event Func<Exception, EndPoint, Task> OnError;
        public event Func<string, Task> OnStopped;

        // Property jika class digunakan independen
        public TcpHeaderType DefaultHeaderType { get; set; } = TcpHeaderType.Binary2Byte;
        public TcpLengthMode DefaultLengthMode { get; set; } = TcpLengthMode.Exclude;
        public TcpEndianMode DefaultEndianMode { get; set; } = TcpEndianMode.BigEndian;

        public bool Persistent { get; set; } = true;
        public bool OneSocketOnly { get; set; }

        public int MaxConnection { get; set; } = 65;
        public int ReadTimeoutMs { get; set; } = 10000; // 10 detik default timeout

        // Private variables and objects
        private readonly ConcurrentDictionary<EndPoint, SemaphoreSlim> _sendLocks = new();
        private readonly ConcurrentDictionary<EndPoint, TcpClient> _clients = new();

        private CancellationTokenSource _cts;
        private TcpListener _listener;
        private int _port;

        public XTcpListener() { }

        public async Task StartAsync(IPAddress ipAddress, int port, CancellationToken cancellationToken = default)
        {
            try
            {
                _port = port;
                _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                _listener = new TcpListener(ipAddress, port);
                _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _listener.Start();

                // Jalankan loop di background
                _ = Task.Run(async () =>
                {
                    while (!_cts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                            var endpoint = client.Client.RemoteEndPoint!;

                            if (OneSocketOnly)
                            {
                                foreach (var existingClient in _clients.Values)
                                {
                                    existingClient.Close();
                                    existingClient.Dispose();
                                }

                                _clients.Clear();
                            }
                            else if (MaxConnection > 0 && _clients.Count >= MaxConnection)
                            {
                                client.Close();
                                client.Dispose();

                                RaiseOnError(
                                    new InvalidOperationException(
                                        $"Maximum connection limit ({MaxConnection}) reached."),
                                    endpoint);

                                continue;
                            }

                            _clients[endpoint] = client;

                            // Trigger OnConnect
                            if (Persistent) RaiseOnConnect(endpoint);

                            _ = Task.Run(() => HandleClientAsync(client, _cts.Token), _cts.Token);
                        }
                        catch (Exception ex)
                        {
                            RaiseOnError(ex, null);
                        }
                    }
                }, _cts.Token);
            }
            catch (Exception ex)
            {
                RaiseOnError(ex, null);
            }

            await Task.CompletedTask;
        }
        public Task StopAsync()
        {
            try
            {
                _cts?.Cancel(); // Hentikan loop background
                _listener.Stop(); // Hentikan listener

                // Tutup semua client aktif
                foreach (var kvp in _clients)
                {
                    var endpoint = kvp.Key;

                    if (_clients.TryRemove(endpoint, out var client))
                    {
                        try
                        {
                            if (_sendLocks.TryRemove(endpoint, out var sendLock))
                            {
                                sendLock.Dispose();
                            }

                            client.Close();
                            client.Dispose();

                            if (Persistent) RaiseOnDisconnect(endpoint);
                        }
                        catch (Exception ex)
                        {
                            RaiseOnError(ex, endpoint);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RaiseOnError(ex, null);
            }

            RaiseOnStopped("Service closed manually");

            return Task.CompletedTask;
        }

        public EndPoint GetEndPointClient()
        {
            return _clients.Values
                .Where(c => c.Connected)
                .Select(c => c.Client.RemoteEndPoint)
                .FirstOrDefault();
        }
        public List<EndPoint> GetListEndPointClient()
        {
            return _clients.Values
                .Where(c => c.Connected)
                .Select(c =>
                {
                    var ep = c.Client.RemoteEndPoint;
                    return ep;
                })
                .Where(ep => ep != null)
                .ToList();
        }

        public bool IsConnected()
        {
            return _clients.Values.Any(c => c.Connected);
        }
        public bool IsConnected(EndPoint endpoint)
        {
            if (_clients.TryGetValue(endpoint, out var client))
            {
                return client.Connected;
            }
            return false;
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            var endpoint = client.Client.RemoteEndPoint!;

            try
            {
                var stream = client.GetStream();

                while (!cancellationToken.IsCancellationRequested && client.Connected)
                {
                    // Cek header length sesuai format yang digunakan
                    int lengthHeader = TcpHeader.GetLengthHeader(DefaultHeaderType);
                    byte[] header = new byte[lengthHeader];

                    try
                    {
                        // Menunggu header masuk dari client.
                        await stream.ReadExactlyAsync(header.AsMemory(0, lengthHeader), cancellationToken);
                    }
                    catch (EndOfStreamException)
                    {
                        break; // Client memutus koneksi secara normal (graceful disconnect)
                    }

                    // Ambil panjang payload dari header
                    int length = TcpHeader.GetLengthMessage(header,
                        DefaultHeaderType, DefaultLengthMode, DefaultEndianMode);

                    // Validasi panjang payload
                    if (length <= 0 || length > MaxPacketSize)
                    {
                        RaiseOnError(new InvalidOperationException("Payload too large"), endpoint);
                        break;
                    }

                    // Baca payload
                    byte[] buffer = new byte[length];

                    // Reset timeout CTS untuk pembacaan payload
                    using var payloadCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    payloadCts.CancelAfter(ReadTimeoutMs);

                    try
                    {
                        await stream.ReadExactlyAsync(buffer.AsMemory(0, length), payloadCts.Token);
                    }
                    catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                    {
                        RaiseOnError(new TimeoutException($"Read timeout ({ReadTimeoutMs}ms) exceeded while reading payload."), endpoint);
                        break; // Putus koneksi jika payload tidak kunjung selesai dikirim
                    }
                    catch (EndOfStreamException)
                    {
                        RaiseOnError(new InvalidDataException("Incomplete payload received (Stream closed prematurely)"), endpoint);
                        break;
                    }

                    // Trigger event OnDataArrival
                    if (Persistent)
                    {
                        // persistent - Fire & forget
                        RaiseOnDataArrival(buffer, endpoint);
                    }
                    else
                    {
                        //non persistent - wait until send completed before closing connection
                        await RaiseOnDataArrivalAsync(buffer, endpoint);
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Abaikan jika pemutusan dipicu oleh cancellationToken saat StopAsync()
            }
            catch (Exception ex)
            {
                RaiseOnError(ex, endpoint);
            }
            finally
            {
                if (_clients.TryRemove(endpoint, out var removedClient))
                {
                    if (_sendLocks.TryRemove(endpoint, out var sendLock))
                    {
                        sendLock.Dispose();
                    }

                    removedClient.Close();
                    removedClient.Dispose();

                    // Trigger OnDisconnect
                    RaiseOnDisconnect(endpoint);
                }
            }
        }

        public async Task SendAsync(string payload, CancellationToken cancellationToken = default)
        {
            await SendAsync(payload.StringToBytes(), cancellationToken);
        }
        public async Task SendAsync(byte[] payload, CancellationToken cancellationToken = default)
        {
            // Kirim ke client yang masih aktif
            foreach (var kvp in _clients)
            {
                var client = kvp.Value;

                if (client.Connected)
                {
                    await SendMessageAsync(payload, client, cancellationToken);
                    break;
                }
            }
        }
        public async Task SendToAsync(string payload, EndPoint endpoint, CancellationToken cancellationToken = default)
        {
            await SendToAsync(payload.StringToBytes(), endpoint, cancellationToken);
        }
        public async Task SendToAsync(byte[] payload, EndPoint endpoint, CancellationToken cancellationToken = default)
        {
            if (_clients.TryGetValue(endpoint, out var client))
            {
                await SendMessageAsync(payload, client, cancellationToken);
            }
            else
            {
                RaiseOnError(new InvalidOperationException("Client not found"), endpoint);
            }
        }
        private async Task SendMessageAsync(byte[] payload, TcpClient client, CancellationToken cancellationToken = default)
        {
            var ep = client.Client.RemoteEndPoint;
            if (ep == null) return;

            // Ambil atau buat lock khusus untuk endpoint ini
            var sendLock = _sendLocks.GetOrAdd(ep, _ => new SemaphoreSlim(1, 1));

            await sendLock.WaitAsync(cancellationToken);

            try
            {
                var stream = client.GetStream();

                // Tambahkan header sesuai format yang digunakan
                byte[] packet = TcpHeader.AddTcpHeader(payload,
                    DefaultHeaderType,
                    DefaultLengthMode,
                    DefaultEndianMode);

                // Kirim sekali
                await stream.WriteAsync(packet.AsMemory(0, packet.Length), cancellationToken);
            }
            catch (Exception ex)
            {
                RaiseOnError(ex, client.Client.RemoteEndPoint);
            }
            finally
            {
                sendLock.Release();
            }
        }

        // === MULTICAST FIRE-AND-FORGET HELPER ===
        // === METODE PEMICU KHUSUS MASING-MASING EVENT (AMBIL & JALANKAN) ===
        private void RaiseOnConnect(EndPoint remoteEndPoint)
        {
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
        private void RaiseOnError(Exception exception, EndPoint ep)
        {
            var handler = OnError;
            if (handler == null) return;

            // Membongkar multicast list khusus untuk Func<Exception, Task>
            foreach (Func<Exception, EndPoint, Task> singleCast in handler.GetInvocationList().Cast<Func<Exception, EndPoint, Task>>())
            {
                _ = Task.Run(async () =>
                {
                    try { await singleCast(exception, ep); }
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
            // Fire and forget
            _ = RaiseOnDataArrivalAsync(data, remoteEndPoint);
        }
        private async Task RaiseOnDataArrivalAsync(byte[] data, EndPoint remoteEndPoint)
        {
            var handler = OnDataArrival;
            if (handler == null) return;

            var tasks = handler.GetInvocationList()
                .Cast<Func<byte[], EndPoint, Task>>()
                .Select(async singleCast =>
                {
                    try { await singleCast(data, remoteEndPoint); }
                    catch (Exception ex) { RaiseOnError(ex, remoteEndPoint); }
                });

            await Task.WhenAll(tasks);
        }
    }
}
