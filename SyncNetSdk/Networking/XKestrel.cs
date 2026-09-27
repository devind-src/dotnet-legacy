using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace SyncNet.Networking
{
    public class XKestrel
    {
        // Async events
        public event Func<string, HttpContext, Task> OnDataArrival;
        public event Func<Exception, Task> OnError;

        // Public properties
        public string Method { get; set; } = "POST";
        public string Content { get; set; } = "application/json";
        public int MaxConcurrentRequests { get; set; } = 100;
        public int MaxWait { get; set; } = 30;

        // Private fields
        private WebApplication _app;

        // Pending requests disimpan dengan RequestId, bukan HttpContext
        private readonly ConcurrentDictionary<string,
            TaskCompletionSource<(string Response, HttpStatusCode StatusCode, Dictionary<string, string> Headers)>> _pendingRequests = new();

        public XKestrel() { }

        public async Task StartAsync(string url)
        {
            try
            {
                var builder = WebApplication.CreateBuilder();
                builder.Logging.ClearProviders();

                _app = builder.Build();
                _app.Urls.Add(url);

                if (string.IsNullOrWhiteSpace(Method))
                {
                    throw new InvalidOperationException("HTTP method wajib diisi.");
                }

                _app.MapMethods("/{*anyPath}", [Method.ToUpperInvariant()], async (HttpContext context) =>
                {
                    var requestId = Guid.NewGuid().ToString("N");
                    context.Items["RequestId"] = requestId;

                    var tcs = new TaskCompletionSource<(string, HttpStatusCode, Dictionary<string, string>)>(
                        TaskCreationOptions.RunContinuationsAsynchronously);

                    _pendingRequests.TryAdd(requestId, tcs);

                    using var registration = context.RequestAborted.Register(() =>
                    {
                        tcs.TrySetCanceled();
                    });

                    try
                    {
                        using var reader = new StreamReader(context.Request.Body);
                        string body = await reader.ReadToEndAsync();

                        if (OnDataArrival != null)
                        {
                            var delegates = OnDataArrival.GetInvocationList();
                            foreach (Func<string, HttpContext, Task> handler in delegates)
                            {
                                _ = Task.Run(async () =>
                                {
                                    try { await handler(body, context); }
                                    catch (Exception ex) { if (OnError != null) await OnError(ex); }
                                });
                            }
                        }

                        // Tunggu balasan dari Reply()
                        var (responseContent, statusCode, headers) =
                            await tcs.Task.WaitAsync(TimeSpan.FromSeconds(MaxWait));

                        // Kirim response ke client
                        context.Response.StatusCode = (int)statusCode;
                        context.Response.ContentType = context.Request.ContentType ?? "application/json";

                        if (headers != null)
                        {
                            foreach (var header in headers)
                            {
                                context.Response.Headers[header.Key] = header.Value;
                            }
                        }

                        await context.Response.WriteAsync(responseContent);
                        await context.Response.CompleteAsync();
                    }
                    catch (TaskCanceledException ex)
                    {
                        //Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]: Client disconnected");
                        //if (OnError != null) await OnError.Invoke(ex);
                    }
                    catch (TimeoutException ex)
                    {
                        //Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]: Timeout! The main application is late in replying");                        
                        //if (OnError != null) await OnError.Invoke(ex);

                        if (!context.Response.HasStarted)
                        {
                            context.Response.StatusCode = StatusCodes.Status408RequestTimeout;
                            await context.Response.CompleteAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]: Error: {ex.Message}");
                        if (OnError != null) await OnError.Invoke(ex);

                        if (!context.Response.HasStarted)
                        {
                            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                            await context.Response.CompleteAsync();
                        }
                    }
                    finally
                    {
                        _pendingRequests.TryRemove(requestId, out _);
                    }
                });

                await _app.StartAsync();
            }
            catch (Exception ex)
            {
                if (OnError != null) await OnError.Invoke(ex);
                throw;
            }
        }

        public async Task StopAsync()
        {
            if (_app != null)
            {
                foreach (var tcs in _pendingRequests.Values)
                {
                    tcs.TrySetCanceled();
                }
                _pendingRequests.Clear();

                await _app.StopAsync();
                await _app.DisposeAsync();
            }
        }

        public async Task Reply(string response, HttpContext context)
        {
            await Reply(response, context, HttpStatusCode.OK);
        }

        public async Task Reply(string response, HttpContext context, HttpStatusCode statusCode,
            Dictionary<string, string> headers = null)
        {
            try
            {
                // Cari requestId dari context.Items
                if (context.Items.TryGetValue("RequestId", out var obj) && obj is string requestId)
                {
                    if (_pendingRequests.TryGetValue(requestId, out var tcs))
                    {
                        tcs.TrySetResult((response, statusCode, headers));
                    }
                    else
                    {
                        throw new InvalidOperationException("Koneksi client sudah terputus atau kadaluarsa.");
                    }
                }
                else
                {
                    throw new InvalidOperationException("RequestId tidak ditemukan di HttpContext.");
                }
            }
            catch (Exception ex)
            {
                if (OnError != null) await OnError.Invoke(ex);
            }

            await Task.CompletedTask;
        }
    }
}
