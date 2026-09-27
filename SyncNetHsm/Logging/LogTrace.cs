using Microsoft.AspNetCore.Http;
using SyncNet.Common;
using SyncNet.Helpers;
using System;
using System.IO;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SyncNet.Logging
{
    public class LogTrace
    {
        private readonly RequestDelegate _next;
        private readonly CustomLogger _logger;
        private readonly Channel<string> _logChannel;

        public LogTrace(RequestDelegate next, CustomLogger logger, Channel<string> logChannel)
        {
            _next = next;
            _logger = logger;
            _logChannel = logChannel;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                string req = await GetRequest(context);
                await _logChannel.Writer.WriteAsync(BuildLog("Request from", context, req));

                string rsp = await GetResponse(context, _next);
                await _logChannel.Writer.WriteAsync(BuildLog("Response to", context, rsp));
            }
            catch { }
        }

        private string BuildLog(string type, HttpContext ctx, string body)
        {
            string ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            string url = ctx.Request.Path;
            return $"[{DateTime.Now:HH:mm:ss.fff}] {type} {ip} {url}\n{body}\n";
        }

        private async Task<string> GetRequest(HttpContext context)
        {
            string requestBody = string.Empty;
            var method = context.Request.Method;

            // Ensure the request body can be read multiple times
            context.Request.EnableBuffering();

            // Only if we are dealing with POST or PUT, GET and others shouldn't have a body
            if (context.Request.Body.CanRead && (method == HttpMethods.Post || method == HttpMethods.Put))
            {
                // Leave stream open so next middleware can read it
                using var reader = new StreamReader(
                    context.Request.Body,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: 512, leaveOpen: true);

                //read message
                requestBody = await reader.ReadToEndAsync();

                // Reset stream position, so next middleware can read it
                context.Request.Body.Position = 0;
            }

            return requestBody;
        }

        private async Task<string> GetResponse(HttpContext context, RequestDelegate next)
        {
            string responseBody = string.Empty;
            var originalBodyStream = context.Response.Body;

            try
            {
                // Swap out stream with one that is buffered and suports seeking
                using var memoryStream = new MemoryStream();
                context.Response.Body = memoryStream;

                // hand over to the next middleware and wait for the call to return
                await next(context);

                // Read response body from memory stream
                memoryStream.Position = 0;
                var reader = new StreamReader(memoryStream);

                //read message
                responseBody = await reader.ReadToEndAsync();

                // Copy body back to so its available to the user agent
                memoryStream.Position = 0;
                await memoryStream.CopyToAsync(originalBodyStream);
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }

            return responseBody;
        }    
    }
}
