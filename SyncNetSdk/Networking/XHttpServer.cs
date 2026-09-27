using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncNet.Networking
{
    public class XHttpServer
    {
        // Async events
        public event Func<string, HttpListenerContext, Task> OnDataArrival;
        public event Func<string, Task> OnError;

        // Private fields
        private HttpListener _listener;
        private SemaphoreSlim _semaphore;
        private CancellationTokenSource _cts;

        public string Method { get; set; } = "POST";
        public string Content { get; set; } = "application/json";
        public int MaxConcurrentRequests { get; set; } = 100;

        public XHttpServer()
        {
            ServicePointManager.ServerCertificateValidationCallback =
                new RemoteCertificateValidationCallback(Certificate.ValidateRemoteCertificate);
        }

        public Task StartAsync(params string[] prefixes)
        {
            if (!HttpListener.IsSupported)
                throw new NotSupportedException("HttpListener not supported on this platform.");

            if (prefixes == null || prefixes.Length == 0)
                throw new ArgumentException("URL is empty");

            try
            {
                _listener = new HttpListener();
                foreach (var prefix in prefixes)
                {
                    var url = prefix.EndsWith("/") ? prefix : prefix + "/";
                    _listener.Prefixes.Add(url);
                }

                _semaphore = new SemaphoreSlim(MaxConcurrentRequests);
                _cts = new CancellationTokenSource();

                _listener.Start();

                // Jalankan loop listener sebagai background task
                _ = Task.Run(() => ListenerLoopAsync(_cts.Token));
            }
            catch (Exception ex)
            {
                if (OnError != null)
                    _ = OnError.Invoke($"Start failed: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            try
            {
                _cts?.Cancel();

                if (_listener != null)
                {
                    _listener.Stop();
                    _listener.Close();
                }

                _semaphore?.Dispose();
            }
            catch (Exception ex)
            {
                if (OnError != null)
                    await OnError.Invoke($"Stop failed: {ex.Message}");
            }

            await Task.CompletedTask; // menjaga konsistensi async signature
        }

        private async Task ListenerLoopAsync(CancellationToken token)
        {
            try
            {
                while (_listener.IsListening && !token.IsCancellationRequested)
                {
                    HttpListenerContext context = null;
                    try
                    {
                        context = await _listener.GetContextAsync();
                        _ = HandleContextAsync(context, token);
                    }
                    catch (HttpListenerException) { /* Listener was stopped */ }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex)
                    {
                        OnError?.Invoke($"ListenerLoopAsync error: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
            }
        }

        private async Task HandleContextAsync(HttpListenerContext context, CancellationToken token)
        {
            await _semaphore.WaitAsync(token);

            try
            {
                await ProcessRequestAsync(context);
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"HandleContext error: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                string msgRequest = string.Empty;

                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    msgRequest = await reader.ReadToEndAsync();
                }

                OnDataArrival?.Invoke(msgRequest, context);
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"ProcessRequest error: {ex.Message}");
            }
        }

        public void Reply(HttpListenerContext ctx, HttpStatusCode code)
        {
            try
            {
                ctx.Response.StatusCode = (int)code;
                ctx.Response.Close();
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Reply(code) error: {ex.Message}");
            }
        }

        public async Task Reply(HttpListenerContext ctx, byte[] bytes)
        {
            try
            {
                ctx.Response.ContentLength64 = bytes.Length;
                await ctx.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                ctx.Response.Close();
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Reply(bytes) error: {ex.Message}");
            }
        }

        public async Task Reply(HttpListenerContext ctx, string msg)
        {
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(msg);
                ctx.Response.ContentLength64 = buffer.Length;
                await ctx.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                ctx.Response.Close();
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Reply(string) error: {ex.Message}");
            }
        }
    }
}
