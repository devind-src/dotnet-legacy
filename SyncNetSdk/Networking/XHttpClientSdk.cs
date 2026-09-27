using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SyncNet.Networking
{
    internal class XHttpClientSdk : IAsyncDisposable
    {
        // Async-friendly events
        public event Func<Message.Request, string, HttpStatusCode, Task> OnDataArrival;
        public event Func<Message.Request, Exception, Task> OnError;

        // Public variable
        public string WsUrl { get; set; }
        public string WsContent { get; set; } = "application/json";
        public string WsMethod { get; set; } = "POST";
        public string WsProxyUrl { get; set; }
        public int WsProxyPort { get; set; }
        public int WsTimeout { get; set; } = 30;

        private readonly HttpClient _httpClient;
        private readonly HttpClientHandler _handler;

        public XHttpClientSdk(string url, string proxyUrl = null, int proxyPort = 0, int timeoutSeconds = 30)
        {
            WsUrl = url;
            WsProxyUrl = proxyUrl;
            WsProxyPort = proxyPort;
            WsTimeout = timeoutSeconds;

            _handler = new HttpClientHandler
            {
                Proxy = string.IsNullOrWhiteSpace(WsProxyUrl) ? null : new WebProxy(WsProxyUrl, WsProxyPort),
                UseProxy = !string.IsNullOrWhiteSpace(WsProxyUrl),
                ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) =>
                    Certificate.ValidateRemoteCertificate(msg, cert, chain, errors)
            };

            _httpClient = new HttpClient(_handler)
            {
                Timeout = TimeSpan.FromSeconds(WsTimeout)
            };
        }

        public async Task Send(Message.Request MsgOriginal, string MsgRequest, WebHeaderCollection Header)
        {
            string Parameter = string.Empty;

            await Send(MsgOriginal, MsgRequest, Parameter, Header);
        }

        public async Task Send(Message.Request MsgOriginal, string MsgRequest, string Parameter, WebHeaderCollection Header)
        {
            try
            {
                var requestUrl = WsUrl + Parameter;
                using var request = new HttpRequestMessage(new HttpMethod(WsMethod), requestUrl);

                // add header
                if (Header != null)
                {
                    foreach (string key in Header.AllKeys)
                    {
                        request.Headers.TryAddWithoutValidation(key, Header[key]);
                    }
                }

                switch (WsMethod.ToUpperInvariant())
                {
                    case "POST":
                        await PostAsync(MsgOriginal, MsgRequest, request);
                        break;
                    case "GET":
                        await GetAsync(MsgOriginal, request);
                        break;
                    default:
                        await SendAsync(MsgOriginal, MsgRequest, request);
                        break;
                }
            }
            catch (Exception ex)
            {
                if (OnError != null)
                    await OnError.Invoke(MsgOriginal, ex);
            }
        }

        private async Task PostAsync(Message.Request MsgOriginal, string MsgRequest, HttpRequestMessage request)
        {
            request.Content = new StringContent(MsgRequest, System.Text.Encoding.UTF8, WsContent);

            var response = await _httpClient.SendAsync(request);
            string responseString = await response.Content.ReadAsStringAsync();

            if (OnDataArrival != null)
                await OnDataArrival.Invoke(MsgOriginal, responseString, response.StatusCode);
        }

        private async Task GetAsync(Message.Request MsgOriginal, HttpRequestMessage request)
        {
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(WsContent));

            var response = await _httpClient.SendAsync(request);
            string responseString = await response.Content.ReadAsStringAsync();

            if (OnDataArrival != null)
                await OnDataArrival.Invoke(MsgOriginal, responseString, response.StatusCode);
        }

        private async Task SendAsync(Message.Request MsgOriginal, string MsgRequest, HttpRequestMessage request)
        {
            request.Content = new StringContent(MsgRequest, System.Text.Encoding.UTF8, WsContent);

            var response = await _httpClient.SendAsync(request);
            string responseString = await response.Content.ReadAsStringAsync();

            if (OnDataArrival != null)
                await OnDataArrival.Invoke(MsgOriginal, responseString, response.StatusCode);
        }

        public async ValueTask DisposeAsync()
        {
            _httpClient.Dispose();

            await Task.CompletedTask;
        }
    }

}
