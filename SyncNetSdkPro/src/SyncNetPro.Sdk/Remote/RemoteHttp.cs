using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace SyncNetPro.Sdk.Remote;

/// <summary>Request HTTP ke sistem eksternal.</summary>
public sealed record RemoteHttpRequest
{
    /// <summary>Method; default <c>ws_method</c> koneksi (atau POST).</summary>
    public string? Method { get; init; }

    /// <summary>Path/query yang ditambahkan ke <c>ws_url</c> (mis. <c>/bill/inquiry</c>), atau URL absolut.</summary>
    public string? Path { get; init; }

    /// <summary>Isi request (untuk GET diabaikan).</summary>
    public string? Body { get; init; }

    /// <summary>Content type; default <c>ws_content</c> koneksi (atau <c>application/json</c>).</summary>
    public string? ContentType { get; init; }

    /// <summary>Header tambahan.</summary>
    public IReadOnlyDictionary<string, string>? Headers { get; init; }

    /// <summary>Batas waktu; default <c>request_timeout</c> node.</summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>Request JSON (Newtonsoft, sama dengan interface lama).</summary>
    public static RemoteHttpRequest Json(string path, object body, IReadOnlyDictionary<string, string>? headers = null) => new()
    {
        Method = "POST",
        Path = path,
        Body = JsonConvert.SerializeObject(body),
        ContentType = "application/json",
        Headers = headers,
    };
}

/// <summary>Response HTTP dari sistem eksternal.</summary>
/// <param name="StatusCode">Status HTTP.</param>
/// <param name="Body">Isi response.</param>
/// <param name="Headers">Header response.</param>
public sealed record RemoteHttpResponse(HttpStatusCode StatusCode, string Body, IReadOnlyDictionary<string, string> Headers)
{
    /// <summary>Status 2xx.</summary>
    public bool IsSuccess => (int)StatusCode is >= 200 and < 300;

    /// <summary>Deserialisasi isi JSON (Newtonsoft).</summary>
    public T? ReadJson<T>() => string.IsNullOrWhiteSpace(Body) ? default : JsonConvert.DeserializeObject<T>(Body);
}

/// <summary>Balasan untuk request HTTP masuk (<see cref="SyncNetInterface.OnHttpRequestAsync"/>).</summary>
public sealed record HttpReply
{
    /// <summary>Status HTTP (default 200).</summary>
    public HttpStatusCode StatusCode { get; init; } = HttpStatusCode.OK;

    /// <summary>Isi.</summary>
    public string Body { get; init; } = string.Empty;

    /// <summary>Content type; default content type request (perilaku SDK lama) atau <c>application/json</c>.</summary>
    public string? ContentType { get; init; }

    /// <summary>Header tambahan.</summary>
    public IReadOnlyDictionary<string, string>? Headers { get; init; }

    /// <summary>Balasan teks/JSON mentah.</summary>
    public static HttpReply Text(string body, HttpStatusCode status = HttpStatusCode.OK, string? contentType = null) =>
        new() { Body = body, StatusCode = status, ContentType = contentType };

    /// <summary>Balasan objek JSON (Newtonsoft).</summary>
    public static HttpReply Json(object body, HttpStatusCode status = HttpStatusCode.OK) =>
        new() { Body = JsonConvert.SerializeObject(body), StatusCode = status, ContentType = "application/json" };

    /// <summary>Balasan hanya status (setara <c>ReplyToHttpServer(..., HttpStatusCode, ...)</c>).</summary>
    public static HttpReply Status(HttpStatusCode status) => new() { StatusCode = status };

    internal byte[] BodyBytes => Encoding.UTF8.GetBytes(Body);
}
