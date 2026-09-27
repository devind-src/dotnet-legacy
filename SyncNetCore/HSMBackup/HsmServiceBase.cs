using SWTCoreLab.Library;
using SWTCoreLab.Models;
using SWTCoreLab.NetSocket;
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SWTCoreLab.HSM
{
    internal class HsmServiceBase
    {
        private SocketClient _client;

        // Store TCS by Correlation ID
        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingRequests = new();

        private string _host;
        private int _port;
        private int _headerLength;
        private int _timeout;

        public int HeaderLength
        {
            get { return _headerLength; }
            set { _headerLength = value; }
        }

        public int Timeout
        {
            get { return _timeout; }
            set { _timeout = value; }
        }

        public bool IsConnected
        {
            get { return _client.Connected(); }
        }

        #region TCP Connection Oriented
        public void Initialize(string host, int port, int headerLength, int timeout)
        {
            //read config
            _host = host;
            _port = port;
            _headerLength = headerLength;
            _timeout = timeout * 1000;

            _client = new SocketClient();
            _client.OnConnect += _client_OnConnect;
            _client.OnDisconnect += _client_OnDisconnect;
            _client.OnDataArrival += _client_OnDataArrival;
            _client.OnError += _client_OnError;

            //connect to hsm
            _client.Connect(_host, _port);
        }
        public void Close()
        {
            _client.Disconnect();
        }

        public async Task<TcpResponse> SendAsync(string message)
        {
            TcpResponse result = new();

            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            //get key
            string correlationId = message.Substring(0, _headerLength);

            // Store TCS with Correlation ID
            _pendingRequests[correlationId] = tcs;

            try
            {
                //log outgoing
                if (MyApp.IsTraceOn() == true)
                {
                    MyApp.Logger($"Send to HSM {_host},{_port}", message);
                }

                //send data
                byte[] data = NbMessage.AddTCPHeader(message, NbMessage.EnumTCPHeader.TCP_2_Bytes_ASC_ExcludeHeader);
                _client.Send(data);

                // wait response
                if (await Task.WhenAny(tcs.Task, Task.Delay(_timeout)) == tcs.Task)
                {
                    // Berhasil menerima respons
                    result.is_success = true;
                    result.resp_data = tcs.Task.Result;

                    return result;
                }
                else
                {
                    // Timeout
                    MyApp.Logger($"Transaction ID {correlationId} timed out.");
                    result.error_message = $"Transaction ID {correlationId} timed out.";

                    return result; //timeout
                }
            }
            catch (Exception ex)
            {
                MyApp.Logger($"HSM service error [ID={correlationId}]: {ex.Message}");
                return result; //error
            }
            finally
            {
                // Hapus TCS setelah selesai
                _pendingRequests.TryRemove(correlationId, out _);
            }
        }

        private void _client_OnDataArrival(byte[] Data, int TotalBytes, System.Net.EndPoint remoteEP)
        {
            try
            {
                string response = NbConvert.ByteToString(Data, 2, TotalBytes - 2);

                //log incoming
                if (MyApp.IsTraceOn() == true)
                {
                    MyApp.Logger($"Reply from HSM {_host},{_port}", response);
                }

                //get key
                string correlationId = response.Substring(0, _headerLength);

                // Cek apakah ada request yang menunggu Correlation ID ini
                if (_pendingRequests.TryGetValue(correlationId, out var tcs))
                {
                    tcs.TrySetResult(response);
                }
                else
                {
                    MyApp.Logger($"Received unexpected response with Correlation ID: {correlationId}");
                }
            }
            catch (Exception ex)
            {
                MyApp.Logger($"HSM service error: {ex.Message}");
            }
        }

        private void _client_OnDisconnect(string IPAddress, string Port)
        {
            MyApp.Logger($"HSM service disconnected from {IPAddress},{Port}");
        }

        private void _client_OnConnect(string IPAddress, string Port)
        {
            MyApp.Logger($"HSM service connected to {IPAddress},{Port}");
        }

        private void _client_OnError(string StackTrace, string Description)
        {
            MyApp.Logger($"HSM service error: {Description}");
        }
        #endregion

        #region TCP Connectionless
        public async Task<TcpResponse> ConnectAndSend(string request)
        {
            TcpResponse result = new();

            try
            {
                //log outgoing
                if (MyApp.IsTraceOn() == true)
                {
                    MyApp.Logger($"Send to HSM {_host},{_port}", request);
                }

                //convert to byte & add tcp header
                byte[] data = NbMessage.AddTCPHeader(request, NbMessage.EnumTCPHeader.TCP_2_Bytes_ASC_ExcludeHeader);

                using var tcpClient = new TcpClient();

                // Connect to the server
                await tcpClient.ConnectAsync(_host, _port);

                // Get the network stream
                using NetworkStream networkStream = tcpClient.GetStream();

                // Send data to the server
                await networkStream.WriteAsync(data, 0, data.Length);

                // Baca 2 byte pertama sebagai header
                byte[] headerBuffer = new byte[2]; // Buffer untuk header 2 byte
                int headerBytesRead = await networkStream.ReadAsync(headerBuffer, 0, headerBuffer.Length);
                if (headerBytesRead == 0)
                {
                    result.error_message = "Stream closed";
                    return result;
                }

                if (headerBytesRead < 2) // Pastikan header lengkap terbaca
                {
                    result.error_message = "Incomplete header received";
                    return result;
                }

                // Konversi header menjadi panjang pesan (big-endian)
                int messageLength = NbConvert.BytesToInt(headerBuffer);

                // Buat buffer untuk membaca pesan
                byte[] messageBuffer = new byte[messageLength];
                int totalBytesRead = 0;

                // Baca pesan sesuai panjang dari header
                while (totalBytesRead < messageLength)
                {
                    int bytesRead = await networkStream.ReadAsync(
                        messageBuffer, totalBytesRead,
                        messageLength - totalBytesRead);

                    // Jika stream ditutup sebelum selesai membaca
                    if (bytesRead == 0)
                    {
                        result.error_message = "Stream closed before full message was read";
                        return result;
                    }

                    totalBytesRead += bytesRead;
                }

                //success
                result.is_success = true;
                result.resp_data = NbConvert.ByteToString(messageBuffer);

                //log incoming
                if (MyApp.IsTraceOn() == true)
                {
                    MyApp.Logger($"Reply from HSM {_host},{_port}", result.resp_data);
                }
            }
            catch (Exception ex)
            {
                result.error_message = $"Error sending data or receiving response: {ex.Message}";
            }

            return result;
        }
        #endregion
    }
}
