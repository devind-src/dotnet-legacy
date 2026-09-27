using System.Net.Sockets;
using System.Threading.Tasks;
using System;
using HsmThalesArtajasa.Library;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using HsmThalesArtajasa.Common;
using HsmThalesArtajasa.Models;

namespace HsmThalesArtajasa.Implementations
{
    internal class ApiServiceClient
    {
        private static TcpClient _tcpClient;
        private static NetworkStream _networkStream;

        private static string _host;
        private static int _port;

        public static int HeaderLength { get; set; } = 4;
        public static bool IsConnected { get; private set; }

        private static ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingTransactions;

        public static async void Initialize()
        {
            _pendingTransactions = new ConcurrentDictionary<string, TaskCompletionSource<string>>();

            await ConnectAsync(AppConfig.Params.HsmIpAddress, AppConfig.Params.HsmPort, AppConfig.Params.HsmLenHeader);
        }

        public static async Task<bool> ConnectAsync(string host, int port, int headerLength)
        {
            try
            {
                _host = host;
                _port = port;

                HeaderLength = headerLength;

                //timeout
                int timeout = AppConfig.Params.HsmTimeout;
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));

                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(host, port, cts.Token);

                _networkStream = _tcpClient.GetStream();
                IsConnected = true;

                Logger.Log($"HSM service connected to {host}:{port}");

                // Start listening for responses in the background
                _ = Task.Run(ReceiveResponsesAsync);
            }
            catch (Exception ex)
            {
                IsConnected = false;
                Logger.Log($"HSM service connection error: {ex.Message}");
            }

            return IsConnected;
        }

        public static void Close()
        {
            try
            {
                IsConnected = false;

                Logger.Log("HSM service disconnected");

                _networkStream?.Close();
                _tcpClient?.Close();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error while closing HSM service: {ex.Message}");
            }
        }

        public static async Task<TcpResponse> SendAsync(string message)
        {
            TcpResponse retval = new TcpResponse();

            int timeoutMilliseconds = AppConfig.Params.HsmTimeout * 1000;

            // Validate message length
            if (string.IsNullOrEmpty(message) || message.Length < HeaderLength + 2)
            {
                return retval;
            }

            // Get header and command
            string header = message.Substring(0, HeaderLength);
            string command = message.Substring(HeaderLength, 2);
            string result = header + command + "96"; //default

            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            // Store the transaction in the pending dictionary
            if (!_pendingTransactions.TryAdd(header, tcs))
            {
                Logger.Log($"Failed to add transaction ID: {header}");
                return retval;
            }

            try
            {
                //log outgoing
                if (AppConfig.Params.Trace == "ON")
                {
                    Logger.Log($"Request to {_host},{_port}", message);
                }

                // Reconnect if the connection is not valid
                if (IsListenerActive() == false) await TryReconnect();
            
                //send to hsm device
                byte[] data = message.AddTcpHeader();
                await _networkStream.WriteAsync(data, 0, data.Length);

                using (var cts = new CancellationTokenSource(timeoutMilliseconds))
                {
                    // Wait for the response or timeout
                    var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMilliseconds, cts.Token));

                    if (completedTask == tcs.Task)
                    {
                        retval.is_success = true;
                        retval.resp_data = await tcs.Task;

                        //log incoming
                        if (AppConfig.Params.Trace == "ON")
                        {
                            Logger.Log($"Response from {_host},{_port}", retval.resp_data);
                        }

                        // Return the response
                        return retval;
                    }
                    else
                    {
                        // Timeout
                        Logger.Log($"Transaction {header} timed out.");

                        retval.error_message = "Timeout";

                        return retval; //timeout
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error in SendTransactionAsync: {ex.Message}");
                return retval;
            }
            finally
            {
                // Remove the transaction from the dictionary
                _pendingTransactions.TryRemove(header, out _);
            }
        }

        private static async Task ReceiveResponsesAsync()
        {
            try
            {
                while (IsConnected)
                {
                    try
                    {
                        // Read the header (2 bytes for length)
                        byte[] headerBuffer = new byte[2];
                        int headerBytesRead = await _networkStream.ReadAsync(headerBuffer, 0, headerBuffer.Length);

                        if (headerBytesRead < 2) continue;

                        int messageLength = headerBuffer.BytesToInt();

                        // Validate message length 
                        if (messageLength <= 0 || messageLength > 10240) // adjust as needed
                        {
                            Logger.Log($"Invalid message length: {messageLength}. Skipping this message.");
                            continue; // Skip processing if message length is invalid
                        }

                        byte[] buffer = new byte[messageLength];

                        int totalBytesRead = 0;
                        int readAttempts = 0; // Track number of attempts to read

                        while (totalBytesRead < messageLength)
                        {
                            try
                            {
                                int bytesRead = await _networkStream.ReadAsync(buffer, totalBytesRead, messageLength - totalBytesRead);
                                if (bytesRead == 0) break; // Connection closed
                                totalBytesRead += bytesRead;
                            }
                            catch (Exception readEx)
                            {
                                // Increment attempt counter and check if we've reached the max attempts
                                readAttempts++;

                                if (readAttempts >= 3) // Max 3 read attempts
                                {
                                    Logger.Log("Maximum read attempts reached. Breaking the loop.");
                                    break; // Exit the loop after 3 failed attempts
                                }

                                // Optionally, add a delay before retrying
                                await Task.Delay(1000); // Delay 1 second before retrying
                            }
                        }

                        if (!IsConnected) break; // Jika reconnect, berhenti membaca

                        string response = NbConvert.BytesToString(buffer);

                        // Extract Transaction ID from response
                        string header = response.Substring(0, HeaderLength);

                        if (_pendingTransactions.TryRemove(header, out var tcs))
                        {
                            // Complete the task with the response data
                            tcs.TrySetResult(response);
                        }
                        else
                        {
                            Logger.Log($"Unexpected Transaction ID: {header}");
                        }
                    }
                    catch (Exception ex)
                    {
                        if (_tcpClient != null && _tcpClient.Connected && _networkStream != null && _networkStream.CanWrite)
                            Logger.Log($"Error in processing response: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error in ReceiveResponsesAsync: {ex.Message}");
            }
        }

        private static async Task TryReconnect()
        {
            try
            {
                Logger.Log($"Try reconnect...");

                Close(); // Tutup koneksi lama jika ada.

                // Coba koneksi ulang.
                await ConnectAsync(_host, _port, HeaderLength);
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to reconnect: {ex.Message}");
            }
        }

        private static bool IsListenerActive()
        {
            IsConnected = _tcpClient != null && _tcpClient.Connected  && _networkStream != null && _networkStream.CanRead;

            return IsConnected;
        }
    }
}
