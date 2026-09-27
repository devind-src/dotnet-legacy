using SyncNet.Common;
using SyncNet.Helpers;
using SyncNet.Library;
using SyncNet.Logging;
using SyncNet.Models;
using SyncNet.Networking;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SyncNet.Services
{
    public class ApiClient
    {
        // Store TCS by Correlation ID
        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingRequests = new();

        private readonly CustomLogger _logger;
        private readonly XTcpClient _client;

        private readonly string _host;
        private readonly int _port;
        private readonly int _headerLength;

        public ApiClient(CustomLogger logger, XTcpClient client)
        {
            _logger = logger;
            _client = client;

            _host = AppConfig.HsmIpAddress;
            _port = AppConfig.HsmPort;
            _headerLength = AppConfig.HsmLenHeader;

            _client.OnConnect += ClientOnConnect;
            _client.OnDisconnect += ClientOnDisconnect;
            _client.OnDataArrival += ClientOnDataArrival;
            _client.OnError += ClientOnError;
            _client.OnStopped += ClientOnStopped;
        }


        #region TCP Connection Oriented
        public async Task Connect()
        {
            //koneksi internal retry setiap 5 detik
            _client.RetryDelaySeconds = 5;

            //connect to hsm
            await _client.ConnectAsync(_host, _port);
        }

        public async Task Disconnect()
        {
            //disconnect from hsm
            await _client.DisconnectAsync();
        }

        public async Task<XTcpResponse> SendAsync(string message)
        {
            XTcpResponse result = new();

            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            //get key
            string correlationId = message.Substring(0, _headerLength);

            // Store TCS with Correlation ID
            _pendingRequests[correlationId] = tcs;

            try
            {
                //log outgoing
                if (AppConfig.TraceOn == true)
                {
                    _logger.Log($"Send to HSM {_host},{_port}", message);
                }

                //send data
                await _client.SendAsync(message);

                // wait response
                int timeout = AppConfig.HsmTimeout * 1000;
                if (await Task.WhenAny(tcs.Task, Task.Delay(timeout)) == tcs.Task)
                {
                    // Berhasil menerima respons
                    result.is_success = true;
                    result.resp_data = tcs.Task.Result;

                    return result;
                }
                else
                {
                    // Timeout
                    _logger.Log($"Transaction ID {correlationId} timed out.");
                    result.error_message = $"Transaction ID {correlationId} timed out.";

                    return result; //timeout
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"HSM service error [ID={correlationId}]: {ex.Message}");
                return result; //error
            }
            finally
            {
                // Hapus TCS setelah selesai
                _pendingRequests.TryRemove(correlationId, out _);
            }
        }

        private Task ClientOnStopped(string msg)
        {
            _logger.Log(msg);

            return Task.CompletedTask;
        }

        private Task ClientOnError(Exception exception)
        {
            _logger.Log($"HSM service error: {exception.Message}");

            return Task.CompletedTask;
        }

        private Task ClientOnDataArrival(byte[] bytes, EndPoint point)
        {
            try
            {
                string response = NbConvert.BytesToString(bytes);

                //log incoming
                if (AppConfig.TraceOn == true)
                {
                    _logger.Log($"Reply from HSM {_host},{_port}", response);
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
                    _logger.Log($"Received unexpected response with Correlation ID: {correlationId}");
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"HSM service error: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        private Task ClientOnDisconnect(EndPoint point)
        {
            _logger.Log($"HSM service disconnected from {NetHelper.GetRemoteEP(point)}");

            return Task.CompletedTask;
        }

        private Task ClientOnConnect(EndPoint point)
        {
            _logger.Log($"HSM service connected to {NetHelper.GetRemoteEP(point)}");

            return Task.CompletedTask;
        }


        #endregion

        #region TCP Connectionless
        public async Task<XTcpResponse> ConnectAndSend(string request)
        {
            XTcpResponse result = new();

            try
            {
                //read config
                string host = AppConfig.HsmIpAddress;
                int port = AppConfig.HsmPort;
                int headerLength = AppConfig.HsmLenHeader;
                int requestTimeout = AppConfig.HsmTimeout;

                //log outgoing
                if (AppConfig.TraceOn == true)
                {
                    _logger.Log($"Send to HSM {host},{port}", request);
                }

                //convert to byte & add tcp header
                byte[] data = NbMessage.AddTCPHeader(request, NbMessage.EnumTCPHeader.TCP_2_Bytes_ASC_ExcludeHeader);

                // Create a TCP client with specified timeouts
                using var tcpClient = new TcpClient
                {
                    SendTimeout = requestTimeout * 1000,
                    ReceiveTimeout = requestTimeout * 1000
                };

                // Set socket options for reuse address
                tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                // Connect to the server
                await tcpClient.ConnectAsync(host, port);

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
                int messageLength = headerBuffer.BytesToInt();

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
                result.resp_data = NbConvert.BytesToString(messageBuffer);

                //log incoming
                if (AppConfig.TraceOn == true)
                {
                    _logger.Log($"Reply from HSM {host},{port}", result.resp_data);
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
