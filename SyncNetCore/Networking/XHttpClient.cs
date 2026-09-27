using SyncNet.Models.Networking;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncNet.Networking
{
    public class XHttpClient
    {
        private static readonly HttpClientHandler _httpHandler = new()
        {
            SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
        };

        private static readonly HttpClient _httpClient = new(_httpHandler);

        public static async Task<WSResponse> PostAsync(string url, string content, string contentType, Dictionary<string, string> headers = null, int timeoutSeconds = 30)
        {
            var rsp = new WSResponse();

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

                //set body content
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = new StringContent(content, Encoding.UTF8, contentType);

                //add headers
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        // Gunakan TryAddWithoutValidation agar aman dari error format header tertentu
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                //post msg
                var response = await _httpClient.SendAsync(request, cts.Token);

                //read response
                rsp.MsgResponse = await response.Content.ReadAsStringAsync();
                rsp.StatusCode = response.StatusCode;
            }
            catch (TaskCanceledException)
            {
                //Suspect: Melebihi batas waktu yang telah ditentukan
                rsp.StatusCode = HttpStatusCode.RequestTimeout;
                rsp.MsgError = "Request timed out.";
            }
            catch (HttpRequestException ex) when (ex.InnerException is SocketException socketEx)
            {
                // Cek secara spesifik kode error jaringannya
                if (socketEx.SocketErrorCode == SocketError.ConnectionRefused ||
                    socketEx.SocketErrorCode == SocketError.HostUnreachable ||
                    socketEx.SocketErrorCode == SocketError.AddressNotAvailable)
                {
                    //gagal konek dan request BELUM terkirim.
                    rsp.StatusCode = HttpStatusCode.BadGateway;
                    rsp.MsgError = "Network unreachable.";
                }
                else
                {
                    //Suspect: ConnectionReset, ConnectionAborted, TimedOut di level soket.
                    rsp.StatusCode = HttpStatusCode.GatewayTimeout;
                    rsp.MsgError = "Gateway timed out.";
                }
            }
            catch (Exception ex)
            {
                rsp.StatusCode = HttpStatusCode.BadRequest;
                rsp.MsgError = ex.Message;
            }

            return rsp;
        }

        public static async Task<WSResponse> PostAsync(string url, string json, Dictionary<string, string> headers = null, int timeoutSeconds = 30)
        {
            var rsp = new WSResponse();

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

                //set body content
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                //add headers
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        // Gunakan TryAddWithoutValidation agar aman dari error format header tertentu
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                //post msg
                var response = await _httpClient.SendAsync(request, cts.Token);

                //read response
                rsp.MsgResponse = await response.Content.ReadAsStringAsync();
                rsp.StatusCode = response.StatusCode;
            }
           catch (TaskCanceledException)
            {
                //Suspect: Melebihi batas waktu yang telah ditentukan
                rsp.StatusCode = HttpStatusCode.RequestTimeout;
                rsp.MsgError = "Request timed out.";
            }
            catch (HttpRequestException ex) when (ex.InnerException is SocketException socketEx)
            {
                // Cek secara spesifik kode error jaringannya
                if (socketEx.SocketErrorCode == SocketError.ConnectionRefused ||
                    socketEx.SocketErrorCode == SocketError.HostUnreachable ||
                    socketEx.SocketErrorCode == SocketError.AddressNotAvailable)
                {
                    //gagal konek dan request BELUM terkirim.
                    rsp.StatusCode = HttpStatusCode.BadGateway;
                    rsp.MsgError = "Network unreachable.";
                }
                else
                {
                    //Suspect: ConnectionReset, ConnectionAborted, TimedOut di level soket.
                    rsp.StatusCode = HttpStatusCode.GatewayTimeout;
                    rsp.MsgError = "Gateway timed out.";
                }
            }
            catch (Exception ex)
            {
                rsp.StatusCode = HttpStatusCode.BadRequest;
                rsp.MsgError = ex.Message;
            }

            return rsp;
        }

        public static async Task<WSResponse> GetAsync(string url, Dictionary<string, string> headers = null, int timeoutSeconds = 30)
        {
            var rsp = new WSResponse();

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

                //gunakan HttpRequestMessage dengan HttpMethod.Get
                using var request = new HttpRequestMessage(HttpMethod.Get, url);

                //add headers
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        // Menggunakan TryAddWithoutValidation agar aman dari error format karakter header
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                //Kirim menggunakan SendAsync
                var response = await _httpClient.SendAsync(request, cts.Token);

                //read response
                rsp.MsgResponse = await response.Content.ReadAsStringAsync();
                rsp.StatusCode = response.StatusCode;
            }
            catch (TaskCanceledException)
            {
                //Suspect: Melebihi batas waktu yang telah ditentukan
                rsp.StatusCode = HttpStatusCode.RequestTimeout;
                rsp.MsgError = "Request timed out.";
            }
            catch (HttpRequestException ex) when (ex.InnerException is SocketException socketEx)
            {
                // Cek secara spesifik kode error jaringannya
                if (socketEx.SocketErrorCode == SocketError.ConnectionRefused ||
                    socketEx.SocketErrorCode == SocketError.HostUnreachable ||
                    socketEx.SocketErrorCode == SocketError.AddressNotAvailable)
                {
                    //gagal konek dan request BELUM terkirim.
                    rsp.StatusCode = HttpStatusCode.BadGateway;
                    rsp.MsgError = "Network unreachable.";
                }
                else
                {
                    //Suspect: ConnectionReset, ConnectionAborted, TimedOut di level soket.
                    rsp.StatusCode = HttpStatusCode.GatewayTimeout;
                    rsp.MsgError = "Gateway timed out.";
                }
            }
            catch (Exception ex)
            {
                rsp.StatusCode = HttpStatusCode.BadRequest;
                rsp.MsgError = ex.Message;
            }

            return rsp;
        }
    }
}
