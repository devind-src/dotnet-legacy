using SyncNet.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncNet.Networking
{
    public class SocketClient : IDisposable
    {
        // === ASYNC EVENTS (Fire-and-Forget Multicast Safe) ===
        public event Func<EndPoint, Task> OnConnect;
        public event Func<EndPoint, Task> OnDisconnect;
        public event Func<Exception, Task> OnError;
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
        public bool AutoReconnect { get; set; } = true;
        public int MaxMessageLength { get; set; } = 8192; // Default 8KB
        public int ReadTimeoutSeconds { get; set; } = 30;
        public int RetryDelaySeconds { get; set; } = 0;
        public byte[] MessageDelimiter { get; set; } = [0xFF];


        // === PRIVATE FIELDS ===
        private Socket _client;
        private IPAddress _ipAddress = IPAddress.Loopback;
        private int _port;

        private bool _isConnected;
        private bool _isDisposed;
        private int _isReconnecting = 0;
        private byte[] _readBuffer = []; //used for tcp with end delimiter

        private CancellationTokenSource _cts;
        private EndPoint _activeRemoteEndPoint;
        private readonly RetryPolicyDelay _retryPolicy = new();
        private readonly SemaphoreSlim _sendLock = new(1, 1);

        public bool Connected => _isConnected;

        public async Task ConnectAsync(string remoteHostName, int remotePort)
        {
            try
            {
                _port = remotePort;

                if (remoteHostName.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                {
                    _ipAddress = IPAddress.Loopback;
                }
                else if (IPAddress.TryParse(remoteHostName, out var parsedIp))
                {
                    _ipAddress = parsedIp;
                }
                else
                {
                    var hostEntry = await Dns.GetHostEntryAsync(remoteHostName);
                    _ipAddress = hostEntry.AddressList[0];
                }

                await ConnectAsync(_ipAddress, _port);
            }
            catch (Exception ex)
            {
                RaiseOnError(ex);
            }
        }
        public async Task ConnectAsync(IPAddress remoteIpAddress, int remotePort)
        {
            if (_isConnected) return;

            try
            {
                _ipAddress = remoteIpAddress;
                _port = remotePort;

                _cts?.Dispose();
                _cts = new CancellationTokenSource();

                _client = new Socket(remoteIpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
                {
                    // Set timeout internal socket
                    ReceiveTimeout = ReadTimeoutSeconds * 1000,
                    SendTimeout = ReadTimeoutSeconds * 1000
                };

                await _client.ConnectAsync(new IPEndPoint(_ipAddress, _port), _cts.Token);
                _isConnected = true;
                _activeRemoteEndPoint = _client.RemoteEndPoint;

                // KONEKSI SUKSES: Reset total status penguncian reconnect
                Interlocked.Exchange(ref _isReconnecting, 0);

                // Trigger event connect
                RaiseOnConnect(_client.RemoteEndPoint);

                // Mulai loop pembacaan data di background thread
                _ = Task.Run(() => ReceiveLoopAsync(_cts.Token), _cts.Token);
            }
            catch (Exception ex)
            {
                RaiseOnError(new InvalidOperationException($"Connection to {_ipAddress}, {_port} failed", ex));

                // KONEKSI GAGAL: Lepas kunci di sini agar percobaan berikutnya bisa masuk
                Interlocked.Exchange(ref _isReconnecting, 0);

                _ = HandleReconnectAsync(); // Reconnect jika gagal tersambung
            }
        }

        public async Task SendAsync(string data)
        {
            await SendAsync(Encoding.UTF8.GetBytes(data));
        }
        public async Task SendAsync(byte[] data)
        {
            if (!_isConnected || _client == null)
            {
                var notConnectedEx = new InvalidOperationException("SendAsync gagal: socket tidak dalam status terhubung.");
                RaiseOnError(notConnectedEx); // tetap beri tahu subscriber event
                throw notConnectedEx;          // dan beri tahu caller langsung, tidak silent
            }

            await _sendLock.WaitAsync();

            try
            {
                // capture lokal, jaga-jaga _client berubah setelah lolos pengecekan di atas
                var client = _client;
                if (client == null)
                {
                    var raceEx = new InvalidOperationException("SendAsync gagal: koneksi terputus sebelum pengiriman dimulai.");
                    RaiseOnError(raceEx);
                    throw raceEx;
                }

                byte[] dataWithHeader = AddTcpHeader(data);
                await SendAllAsync(_client, dataWithHeader, _cts?.Token ?? CancellationToken.None);
            }
            catch (Exception ex)
            {
                RaiseOnError(ex);
                _ = HandleReconnectAsync();

                throw; // lempar ulang supaya caller SendAsync tahu pengiriman gagal
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public Task DisconnectAsync()
        {
            try { _cts?.Cancel(); } catch { }

            if (!_isConnected) return Task.CompletedTask;
            _isConnected = false;

            try
            {
                if (_client != null)
                {
                    if (_client.Connected)
                        _client.Shutdown(SocketShutdown.Both);

                    _client.Dispose();
                    _client = null;
                }
            }
            catch (Exception ex)
            {
                RaiseOnError(ex);
            }
            finally
            {
                RaiseOnDisconnect(null);
            }

            // clear buffer
            _readBuffer = [];

            return Task.CompletedTask;
        }
        public bool IsConnected()
        {
            return _isConnected;
        }
        public void SetProtocol()
        {
            //not implemented yet, reserved for future use
        }

        // === CORE LOGIC: RECEIVE LOOP ===
        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            byte[] headerBuffer = new byte[4]; // Alokasi maksimum untuk buffer header

            try
            {
                while (_isConnected && _client != null && !cancellationToken.IsCancellationRequested)
                {
                    // capture referensi lokal — hindari race jika _client di-null-kan thread lain
                    var client = _client;
                    if (client == null) break;

                    if (HeaderLength > 0)
                    {
                        // pastikan tidak ada sisa byte dari iterasi/pesan sebelumnya
                        Array.Clear(headerBuffer);

                        // 1. BACA HEADER: Menunggu sampai tepat sebesar HeaderLength terpenuhi
                        await ReceiveExactlyAsync(client, headerBuffer.AsMemory(0, HeaderLength), cancellationToken);

                        // 2. Parsing Header
                        int msgLength = ParseHeaderLength(headerBuffer);

                        // 3. Validasi Batas Panjang Data (Jika korup -> Lempar Exception -> Auto Reconnect)
                        if (msgLength <= 0 || msgLength > MaxMessageLength)
                        {
                            throw new InvalidDataException($"Invalid data length ({msgLength} byte), indication of corrupted data.");
                        }

                        // 4. BACA BODY: Menunggu sampai seluruh sisa body selesai terunduh
                        byte[] dataBuffer = new byte[msgLength];
                        await ReceiveExactlyAsync(client, dataBuffer.AsMemory(), cancellationToken);

                        // 5. Raise event data saja tanpa header
                        RaiseOnDataArrival(dataBuffer, _activeRemoteEndPoint);
                    }
                    else
                    {
                        // Jika HeaderLength == 0, baca berbasis delimiter (bukan single-shot receive)
                        byte[] rawBuffer = new byte[MaxMessageLength];
                        int msgLength = await ReceiveUntilDelimiterAsync(client, rawBuffer, MessageDelimiter, cancellationToken);

                        byte[] actualData = new byte[msgLength];
                        Array.Copy(rawBuffer, actualData, msgLength);

                        RaiseOnDataArrival(actualData, _activeRemoteEndPoint);
                    }
                }
            }
            catch (Exception ex)
            {
                if (_isConnected && !cancellationToken.IsCancellationRequested)
                {
                    RaiseOnError(new InvalidOperationException("Connection closed by remote host"));

                    _ = HandleReconnectAsync();
                }
            }
        }

        // === UTILITY: REPLIKASI PEMBACAAN PASTI (EXACTLY) ===
        private static async Task ReceiveExactlyAsync(Socket socket, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            int totalBytesRead = 0;
            int bytesToRead = buffer.Length;

            while (totalBytesRead < bytesToRead)
            {
                // Membaca sisa byte yang belum terpenuhi menggunakan SocketFlags.None
                int bytesRead = await socket.ReceiveAsync(buffer[totalBytesRead..], SocketFlags.None, cancellationToken);

                // Jika mengembalikan 0, berarti koneksi terputus di tengah jalan sebelum paket lengkap
                if (bytesRead == 0)
                {
                    throw new SocketException((int)SocketError.ConnectionReset);
                }

                totalBytesRead += bytesRead;
            }
        }
        private async Task<int> ReceiveUntilDelimiterAsync(Socket socket, byte[] outputBuffer, byte[] delimiter, CancellationToken token)
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

        // === UTILITY: EFISIENSI PARSING BINARY HEADER ===
        private int ParseHeaderLength(ReadOnlySpan<byte> header)
        {
            int length = 0;

            if (HeaderLength == 4)
            {
                // Parsing ASCII 4-byte langsung dari Span memori
                string lenStr = Encoding.ASCII.GetString(header[..4]);
                _ = int.TryParse(lenStr, out length);
            }
            else if (HeaderLength == 2)
            {
                int h1 = HeaderHiLo ? header[0] : header[1];
                int h2 = HeaderHiLo ? header[1] : header[0];

                if (HeaderFormat == SdkTcpHeaderFormat.BCD)
                {
                    _ = int.TryParse(h1.ToString("X2") + h2.ToString("X2"), out length);
                }
                else
                {
                    length = (h1 * 256) + h2;
                }
            }
            else
            {
                throw new InvalidOperationException($"HeaderLength tidak valid ({HeaderLength}). Hanya mendukung 2 atau 4.");
            }

            // Penyesuaian jika header dihitung masuk ke dalam total panjang paket
            if (HeaderLengthMode == SdkTcpHeaderLengthMode.IncludeHeader)
            {
                length -= HeaderLength;
            }

            return length;
        }

        // === SECURITY: ALUR RECONNECT AMAN ===
        private async Task HandleReconnectAsync()
        {
            if (!AutoReconnect) return;

            // Gunakan Interlocked untuk menjamin hanya ada 1 loop reconnect yang berjalan
            if (Interlocked.CompareExchange(ref _isReconnecting, 1, 0) != 0) return;

            await DisconnectAsync();

            if (RetryDelaySeconds == 0)
            {
                // Jeda waktu sebelum mencoba re-koneksi kembali (menghindari spamming loop)
                var delaySec = _retryPolicy.GetCurrentDelaySeconds();
                await Task.Delay(delaySec);
                _retryPolicy.Increment();
            }
            else
            {
                // re-koneksi setiap x detik
                var delaySec = TimeSpan.FromSeconds(RetryDelaySeconds);
                await Task.Delay(delaySec);
            }

            // Fire -and-forget reconnect, biarkan event loop utama tetap responsif
            _ = ConnectAsync(_ipAddress, _port);
        }

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
            if (remoteEndPoint == null) remoteEndPoint = new IPEndPoint(_ipAddress, _port);

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

        // === IDISPOSABLE PATTERN ===
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _isConnected = false;
            _cts?.Cancel();
            _cts?.Dispose();
            _client?.Dispose();
            _sendLock?.Dispose();

            GC.SuppressFinalize(this);
        }
    }

    // Class pembantu pendukung
    public class StateObject
    {
        public Socket workSocket = null;
        public const int BufferSize = 8192;
        public byte[] buffer = new byte[BufferSize];
    }
}
