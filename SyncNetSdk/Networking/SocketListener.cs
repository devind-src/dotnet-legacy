using SyncNet.Helpers;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncNet.Networking
{
    public class SocketListener : IDisposable
    {
        // === ASYNC EVENTS (Fire-and-Forget Multicast Safe) ===
        public event Func<EndPoint, Task> OnConnect;
        public event Func<EndPoint, Task> OnDisconnect;
        public event Func<Exception, EndPoint, Task> OnError;
        public event Func<byte[], EndPoint, Task> OnDataArrival;

        // === CONFIGURATIONS ===
        public SdkTcpHeaderLengthMode HeaderLengthMode { get; set; } = SdkTcpHeaderLengthMode.ExcludeHeader;
        public SdkTcpHeaderFormat HeaderFormat { get; set; } = SdkTcpHeaderFormat.ASC;

        // valid header length : 0, 2 & 4
        private byte _headerLength = 2;
        public byte HeaderLength
        {
            get => _headerLength;
            set
            {
                if (value != 0 && value != 2 && value != 4)
                    throw new ArgumentOutOfRangeException(nameof(HeaderLength), value,
                        "HeaderLength hanya boleh 0 (delimiter mode), 2, atau 4.");
                _headerLength = value;
            }
        }

        public bool HeaderHiLo { get; set; } = true;
        public bool AlwaysConnected { get; set; } = true;
        public bool UnlimitedConnection { get; set; } = false;
        public bool OneSocketOnly { get; set; } = true;
        public int MaxLengthMessage { get; set; } = 8192; // Default 8KB
        public byte[] MessageDelimiter { get; set; } = [0xFF];

        // === PRIVATE FIELDS ===
        private bool _isRunning;
        private int _maxConn = 6500;
        private byte[] _readBuffer = []; //used for tcp with end delimiter
        private Socket _listenerSocket;
        private CancellationTokenSource _cts;
        private bool _disposedValue;
        private readonly ConcurrentDictionary<string, Socket> _listClients = new();

        public async Task StartAsync(IPAddress ipAddress, int localPort, int maxConnection = 65)
        {
            try
            {
                if (_isRunning) return;

                _maxConn = maxConnection;
                _cts = new CancellationTokenSource();

                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, localPort);
                _listenerSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _listenerSocket.Bind(localEndPoint);
                _listenerSocket.Listen(100);

                _isRunning = true;

                // Memulai background task untuk menerima koneksi masuk
                _ = Task.Run(() => AcceptConnectionsAsync(_cts.Token));
            }
            catch (Exception ex)
            {
                RaiseOnError(ex, null);
            }
        }
        public async Task StopAsync()
        {
            try
            {
                if (!_isRunning) return;
                _isRunning = false;
                _cts?.Cancel();
                ReleaseAllClients();
                try { _listenerSocket?.Close(); } catch { }
            }
            catch { /* nothing */ }
        }

        private async Task AcceptConnectionsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _isRunning)
            {
                try
                {
                    Socket clientSocket = await _listenerSocket.AcceptAsync(token);
                    _ = Task.Run(() => HandleClientAsync(clientSocket, token), token);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException || ex is ObjectDisposedException))
                {
                    RaiseOnError(ex, null);
                }
            }
        }
        private async Task HandleClientAsync(Socket client, CancellationToken token)
        {
            string clientKey = client.RemoteEndPoint?.ToString() ?? Guid.NewGuid().ToString();
            EndPoint remoteEndPoint = client.RemoteEndPoint;

            try
            {
                if (OneSocketOnly) ReleaseAllClients();
                else RemoveInactiveSockets();

                if (!UnlimitedConnection && _listClients.Count >= _maxConn)
                {
                    RaiseOnError(new InvalidOperationException("Max connection exceeded"), remoteEndPoint);
                    CloseClientSocket(client);
                    return;
                }

                _listClients.TryAdd(clientKey, client);

                // Persistent connection
                if (AlwaysConnected) RaiseOnConnect(remoteEndPoint);

                // Loop utama pembacaan data per client
                while (_isRunning && client.Connected && !token.IsCancellationRequested)
                {
                    int payloadLength = 0;

                    // Baca Header jika konfigurasi > 0
                    if (HeaderLength > 0)
                    {
                        byte[] headerBuffer = new byte[HeaderLength];

                        try
                        {
                            await ReadExactAsync(client, headerBuffer, HeaderLength, token);
                        }
                        catch (Exception ex) when (ex is IOException ||
                            ex is SocketException || ex is ObjectDisposedException)
                        {
                            break; // Keluar loop secara bersih jika soket ditutup oleh client/server
                        }

                        payloadLength = ParseHeaderLength(headerBuffer);

                        // Validasi: Jika ukuran tidak wajar, paksa disconnect agar client reconnect ulang
                        if (payloadLength <= 0 || payloadLength > MaxLengthMessage)
                        {
                            RaiseOnError(new InvalidOperationException(
                                $"Invalid message length detected ({payloadLength} bytes). Forcing reconnection."),
                                remoteEndPoint);
                            break;
                        }

                        // Baca Payload Isi Data sebesar payloadLength
                        byte[] dataBuffer = new byte[payloadLength];
                        await ReadExactAsync(client, dataBuffer, payloadLength, token);

                        // Hanya Raise Isinya saja tanpa Header
                        RaiseOnDataArrival(dataBuffer, client.RemoteEndPoint);
                    }
                    else
                    {
                        // Jika HeaderLength == 0, baca berbasis delimiter (bukan single-shot receive)
                        byte[] rawBuffer = new byte[MaxLengthMessage];
                        int msgLength = await ReadUntilDelimiterAsync(client, rawBuffer, MessageDelimiter, token);

                        byte[] actualData = new byte[msgLength];
                        Array.Copy(rawBuffer, actualData, msgLength);

                        RaiseOnDataArrival(actualData, client.RemoteEndPoint);
                    }

                    // Non Persisten langsung putuskan koneksi agar client reconnect ulang 
                    if (!AlwaysConnected)
                    {
                        // Menunggu sampai pembatalan token atau soket ditutup dari luar
                        await Task.Delay(Timeout.Infinite, token);
                    }
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException || ex is ObjectDisposedException || ex is IOException))
            {
                RaiseOnError(ex, remoteEndPoint);
            }
            finally
            {
                // Proses pembersihan saat terputus atau terjadi eror validasi
                _listClients.TryRemove(clientKey, out _);

                CloseClientSocket(client);

                // Persistent Connection
                if (AlwaysConnected) RaiseOnDisconnect(remoteEndPoint);
            }
        }
        private async Task ReadExactAsync(Socket socket, byte[] buffer, int length, CancellationToken token)
        {
            int totalRead = 0;
            while (totalRead < length)
            {
                int read = await socket.ReceiveAsync(new Memory<byte>(buffer, totalRead, length - totalRead), SocketFlags.None, token);
                if (read == 0) throw new IOException("Koneksi terputus saat membaca segmen data.");
                totalRead += read;
            }
        }
        private async Task<int> ReadUntilDelimiterAsync(Socket socket, byte[] outputBuffer, byte[] delimiter, CancellationToken token)
        {
            if (delimiter == null || delimiter.Length == 0)
                throw new ArgumentException("MessageDelimiter harus diisi minimal 1 byte untuk mode delimiter.");

            // Mulai dari sisa overflow pesan sebelumnya (kalau ada, dari pesan lain yang sudah kebaca duluan)
            int totalBuffered = _readBuffer.Length;
            if (totalBuffered > outputBuffer.Length)
                throw new InvalidOperationException($"Overflow buffer ({totalBuffered} byte) melebihi batas MaxMessageLength ({outputBuffer.Length} byte).");

            Array.Copy(_readBuffer, outputBuffer, totalBuffered);
            _readBuffer = Array.Empty<byte>();

            // Cek dulu barangkali delimiter sudah ada di dalam overflow (pesan sebelumnya bawa >1 pesan sekaligus)
            int delimiterIndex = FindDelimiterIndex(outputBuffer, totalBuffered, delimiter);

            byte[] chunk = new byte[8192];

            while (delimiterIndex < 0)
            {
                int bytesRead = await socket.ReceiveAsync(chunk.AsMemory(), SocketFlags.None, token);
                if (bytesRead == 0) throw new IOException("Koneksi terputus saat membaca data.");

                if (totalBuffered + bytesRead > outputBuffer.Length)
                    throw new InvalidOperationException($"Pesan melebihi batas buffer ({outputBuffer.Length} byte) tanpa end delimiter.");

                Array.Copy(chunk, 0, outputBuffer, totalBuffered, bytesRead);

                // Cari delimiter hanya mulai dari sedikit sebelum data baru (jaga-jaga delimiter terpotong di batas chunk)
                int searchStart = Math.Max(0, totalBuffered - (delimiter.Length - 1));
                totalBuffered += bytesRead;

                delimiterIndex = FindDelimiterIndex(outputBuffer, totalBuffered, delimiter, searchStart);
            }

            int msgLength = delimiterIndex;
            int consumedLength = delimiterIndex + delimiter.Length;

            // Simpan sisa byte setelah delimiter (kalau ada pesan berikutnya yang sudah ikut terbaca) untuk panggilan berikutnya
            int leftover = totalBuffered - consumedLength;
            if (leftover > 0)
            {
                _readBuffer = new byte[leftover];
                Array.Copy(outputBuffer, consumedLength, _readBuffer, 0, leftover);
            }

            return msgLength;
        }
        private static int FindDelimiterIndex(byte[] buffer, int length, byte[] delimiter, int searchStart = 0)
        {
            if (length < delimiter.Length) return -1;

            var span = buffer.AsSpan(0, length);
            int idx = span[searchStart..].IndexOf(delimiter);
            return idx < 0 ? -1 : idx + searchStart;
        }
        private int ParseHeaderLength(byte[] header)
        {
            if (HeaderLength == 4)
            {
                string lenStr = Encoding.ASCII.GetString(header).Trim();
                int len = int.TryParse(lenStr, out int res) ? res : 0;
                return HeaderLengthMode == SdkTcpHeaderLengthMode.ExcludeHeader ? len : len - 4;
            }
            else if (HeaderLength == 2)
            {
                int h1 = HeaderHiLo ? header[0] : header[1];
                int h2 = HeaderHiLo ? header[1] : header[0];

                int totalLen = (HeaderFormat == SdkTcpHeaderFormat.BCD)
                    ? Convert.ToInt32(Convert.ToString(h1, 16).PadLeft(2, '0') + Convert.ToString(h2, 16).PadLeft(2, '0'))
                    : (h1 * 256) + h2;

                return HeaderLengthMode == SdkTcpHeaderLengthMode.ExcludeHeader ? totalLen : totalLen - HeaderLength;
            }
            else
            {
                throw new InvalidOperationException($"HeaderLength tidak valid ({HeaderLength}). Hanya mendukung 2 atau 4.");
            }
        }
        public async Task SendToAsync(byte[] data, EndPoint remoteEP)
        {
            if (_listClients.TryGetValue(remoteEP.ToString(), out Socket client) && client.Connected)
            {
                try
                {
                    byte[] dataWithHeader = AddTcpHeader(data);
                    //int sent = await client.SendAsync(dataWithHeader, SocketFlags.None);
                    await SendAllAsync(client, dataWithHeader, _cts?.Token ?? CancellationToken.None);
                }
                catch (Exception ex)
                {
                    RaiseOnError(ex, remoteEP);
                }
                finally
                {
                    // Non Persistent: Putus koneksi segera setelah response selesai dikirim ke client
                    if (!AlwaysConnected)
                    {
                        _listClients.TryRemove(remoteEP.ToString(), out _);

                        CloseClientSocket(client);
                    }
                }
            }
        }

        private void RemoveInactiveSockets()
        {
            foreach (var pair in _listClients)
            {
                if (!pair.Value.Connected)
                {
                    if (_listClients.TryRemove(pair.Key, out Socket s)) CloseClientSocket(s);
                }
            }
        }
        private void ReleaseAllClients()
        {
            foreach (var pair in _listClients)
            {
                CloseClientSocket(pair.Value);
            }
            _listClients.Clear();
        }
        private void CloseClientSocket(Socket socket)
        {
            try
            {
                if (socket == null) return;

                if (socket.Connected)
                {
                    socket.LingerState = new LingerOption(true, 0);
                    socket.Shutdown(SocketShutdown.Both);
                }

                socket.Close();
            }
            catch { }
        }

        // === UTILITY: UNTUK MEMASTIKAN SEMUA DATA TERKIRIM ===
        private static async Task SendAllAsync(Socket socket, byte[] data, CancellationToken cancellationToken)
        {
            int totalSent = 0;

            while (totalSent < data.Length)
            {
                int sent = await socket.SendAsync(
                    data.AsMemory(totalSent),
                    SocketFlags.None,
                    cancellationToken);

                if (sent == 0)
                    throw new IOException("Koneksi terputus saat mengirim data.");

                totalSent += sent;
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

        // === TCP HEADER HELPER ===
        private byte[] AddTcpHeader(byte[] data)
        {
            byte[] result = [];

            if (HeaderLength == 4)
            {
                if (HeaderLengthMode == SdkTcpHeaderLengthMode.IncludeHeader)
                    result = TcpHelper.AddTcpHeader(data, SdkTcpHeader.Tcp4ByteIncludeHeader);
                else
                    result = TcpHelper.AddTcpHeader(data, SdkTcpHeader.Tcp4ByteExcludeHeader);
            }
            else
            {
                if (HeaderFormat == SdkTcpHeaderFormat.BCD)
                {
                    if (HeaderLengthMode == SdkTcpHeaderLengthMode.IncludeHeader)
                        result = TcpHelper.AddTcpHeader(data, SdkTcpHeader.Tcp2ByteBcdIncludeHeader);
                    else
                        result = TcpHelper.AddTcpHeader(data, SdkTcpHeader.Tcp2ByteBcdExcludeHeader);
                }
                else
                {
                    if (HeaderLengthMode == SdkTcpHeaderLengthMode.IncludeHeader)
                        result = TcpHelper.AddTcpHeader(data, SdkTcpHeader.Tcp2ByteAscIncludeHeader);
                    else
                        result = TcpHelper.AddTcpHeader(data, SdkTcpHeader.Tcp2ByteAscExcludeHeader);
                }
            }

            return result;
        }

        public void Close() => Dispose();
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _isRunning = false;
                    _cts?.Cancel();

                    ReleaseAllClients();

                    try { _listenerSocket?.Close(); } catch { }
                    _cts?.Dispose();
                }
                _disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
