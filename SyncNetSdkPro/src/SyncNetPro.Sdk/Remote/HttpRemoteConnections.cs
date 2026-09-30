using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SyncNetPro.Sdk.Nodes;

namespace SyncNetPro.Sdk.Remote;

/// <summary>
/// HTTP klien ke sistem eksternal (<c>protocol = 7</c>, <c>conn_type = 1</c>). Validasi sertifikat TLS aktif
/// kecuali <see cref="RemoteOptions.AllowUntrustedCertificates"/> (perbaikan B6). Tidak ada retry otomatis.
/// </summary>
internal sealed class HttpClientConnection : RemoteConnectionBase, IRemoteHttpClient
{
    private readonly HttpClient _client;
    private readonly ILogger _logger;

    public HttpClientConnection(RemoteConnectionInfo info, NodeInfo node, bool allowUntrustedCertificates, TimeSpan connectTimeout, ILogger logger)
        : base(info, node)
    {
        if (!Uri.TryCreate(info.WsUrl, UriKind.Absolute, out _))
            throw new InvalidOperationException($"Koneksi {info.Name}: ws_url tidak valid ({info.WsUrl}).");

        _logger = logger;
        var handler = new SocketsHttpHandler
        {
            ConnectTimeout = connectTimeout,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            UseProxy = !string.IsNullOrWhiteSpace(info.WsProxyUrl),
            Proxy = string.IsNullOrWhiteSpace(info.WsProxyUrl) ? null
                : info.WsProxyPort > 0 ? new WebProxy(info.WsProxyUrl, info.WsProxyPort) : new WebProxy(info.WsProxyUrl),
        };

        if (allowUntrustedCertificates)
        {
            logger.LogWarning("Koneksi {Connection}: validasi sertifikat TLS DINONAKTIFKAN (Remote:AllowUntrustedCertificates)", info.Name);
#pragma warning disable CA5359 // opt-in eksplisit khusus pengembangan; default aman (perbaikan B6)
            handler.SslOptions = new SslClientAuthenticationOptions { RemoteCertificateValidationCallback = (_, _, _, _) => true };
#pragma warning restore CA5359
        }

        _client = new HttpClient(handler, disposeHandler: true) { Timeout = Timeout.InfiniteTimeSpan };
    }

    public override bool IsConnected => true;

    public override string? RemoteAddress => Info.WsUrl;

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Interface {Node} siap, url klien -> {Url}", Node.Name, Info.WsUrl);
        return Task.CompletedTask;
    }

    public async Task<RemoteHttpResponse> SendAsync(RemoteHttpRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        string method = (request.Method ?? (string.IsNullOrWhiteSpace(Info.WsMethod) ? "POST" : Info.WsMethod)).ToUpperInvariant();
        string contentType = request.ContentType ?? (string.IsNullOrWhiteSpace(Info.WsContent) ? "application/json" : Info.WsContent);
        Uri url = BuildUrl(Info.WsUrl!, request.Path);

        using var message = new HttpRequestMessage(new HttpMethod(method), url);
        if (method == "GET")
        {
            message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeHeaderValue.Parse(contentType).MediaType!));
        }
        else
        {
            message.Content = new StringContent(request.Body ?? string.Empty, Encoding.UTF8);
            message.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            if (message.Content.Headers.ContentType.CharSet is null) message.Content.Headers.ContentType.CharSet = "utf-8";
        }

        if (request.Headers is not null)
        {
            foreach ((string key, string value) in request.Headers)
            {
                if (!message.Headers.TryAddWithoutValidation(key, value)) message.Content?.Headers.TryAddWithoutValidation(key, value);
            }
        }

        TimeSpan timeout = request.Timeout ?? DefaultTimeout;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);
        try
        {
            using HttpResponseMessage response = await _client.SendAsync(message, cts.Token).ConfigureAwait(false);
            string body = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
            var headers = response.Headers.Concat(response.Content.Headers)
                .ToDictionary(h => h.Key, h => string.Join(",", h.Value), StringComparer.OrdinalIgnoreCase);
            return new RemoteHttpResponse(response.StatusCode, body, headers);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested && !cts.IsCancellationRequested)
        {
            // Dibatalkan oleh handler (ConnectTimeout), bukan batas waktu request: koneksi gagal dibentuk,
            // request belum terkirim → tidak tersedia (bukan timeout balasan yang perlu reversal).
            throw new RemoteUnavailableException($"Tidak dapat terhubung ke {Info.Name} ({url}): {ex.Message}", ex);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Tidak ada balasan dari {Info.Name} ({url}) dalam {timeout.TotalSeconds:0.#} detik.");
        }
        catch (HttpRequestException ex)
        {
            throw new RemoteUnavailableException($"Request ke {Info.Name} ({url}) gagal: {ex.Message}", ex);
        }
    }

    /// <summary>Gabung base URL dan path (SDK lama: <c>WsUrl + Parameter</c>).</summary>
    internal static Uri BuildUrl(string baseUrl, string? path)
    {
        if (string.IsNullOrEmpty(path)) return new Uri(baseUrl);
        if (Uri.TryCreate(path, UriKind.Absolute, out Uri? absolute) && absolute.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return absolute;
        if (path.StartsWith('?')) return new Uri(baseUrl + path);
        return new Uri(baseUrl.TrimEnd('/') + "/" + path.TrimStart('/'));
    }

    public override ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }
}

/// <summary>Dispatcher request HTTP masuk ke handler.</summary>
internal interface IHttpRequestDispatcher
{
    Task<HttpReply> DispatchAsync(HttpServerConnection connection, HttpContext http, CancellationToken cancellationToken);
}

/// <summary>
/// HTTP server (<c>protocol = 7</c>, <c>conn_type = 0</c>) di atas Kestrel, mendengarkan <c>ws_url</c> untuk
/// method <c>ws_method</c> di semua path. Request diteruskan ke <see cref="SyncNetInterface.OnHttpRequestAsync"/>
/// dan balasannya langsung ditulis (tanpa cache <c>HttpContext</c>). Handler yang melewati
/// <c>request_timeout</c> dibalas 408 (perilaku SDK lama).
/// </summary>
internal sealed class HttpServerConnection : RemoteConnectionBase
{
    private readonly IHttpRequestDispatcher _dispatcher;
    private readonly ILogger _logger;
    private WebApplication? _app;

    public HttpServerConnection(RemoteConnectionInfo info, NodeInfo node, IHttpRequestDispatcher dispatcher, ILogger logger)
        : base(info, node)
    {
        if (!Uri.TryCreate(info.WsUrl, UriKind.Absolute, out _))
            throw new InvalidOperationException($"Koneksi {info.Name}: ws_url tidak valid ({info.WsUrl}).");
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public override bool IsConnected => _app is not null;

    public override string? RemoteAddress => Info.WsUrl;

    /// <summary>Alamat aktual yang didengarkan (berguna bila port 0).</summary>
    public IReadOnlyCollection<string> Addresses =>
        _app?.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()?.Addresses.ToArray() ?? [];

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls(Info.WsUrl!);
        WebApplication app = builder.Build();

        string method = string.IsNullOrWhiteSpace(Info.WsMethod) ? "POST" : Info.WsMethod.ToUpperInvariant();
        app.MapMethods("/{**path}", [method], HandleAsync);
        await app.StartAsync(cancellationToken).ConfigureAwait(false);
        _app = app;
        _logger.LogInformation("Interface {Node} listening on {Url}", Node.Name, string.Join(", ", Addresses));
    }

    private async Task HandleAsync(HttpContext http)
    {
        TimeSpan timeout = DefaultTimeout;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(http.RequestAborted);
        HttpReply reply;
        try
        {
            reply = await _dispatcher.DispatchAsync(this, http, cts.Token).WaitAsync(timeout, http.RequestAborted).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            await cts.CancelAsync().ConfigureAwait(false);
            _logger.LogWarning("Request {Path} ke {Connection} tidak dibalas dalam {Timeout} detik; dibalas 408", http.Request.Path, Info.Name, timeout.TotalSeconds);
            reply = HttpReply.Status(HttpStatusCode.RequestTimeout);
        }
        catch (OperationCanceledException) when (http.RequestAborted.IsCancellationRequested)
        {
            return; // klien memutus koneksi
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error memproses request {Path} di {Connection}", http.Request.Path, Info.Name);
            reply = HttpReply.Status(HttpStatusCode.InternalServerError);
        }

        http.Response.StatusCode = (int)reply.StatusCode;
        if (reply.Headers is not null)
        {
            foreach ((string key, string value) in reply.Headers) http.Response.Headers[key] = value;
        }

        if (reply.Body.Length > 0)
        {
            http.Response.ContentType = reply.ContentType ?? http.Request.ContentType ?? "application/json";
            await http.Response.Body.WriteAsync(reply.BodyBytes, http.RequestAborted).ConfigureAwait(false);
        }
    }

    public override async ValueTask DisposeAsync()
    {
        if (_app is null) return;
        await _app.StopAsync().ConfigureAwait(false);
        await _app.DisposeAsync().ConfigureAwait(false);
        _app = null;
    }
}
