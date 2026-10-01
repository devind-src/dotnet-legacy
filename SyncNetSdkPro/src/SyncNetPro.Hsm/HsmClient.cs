using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SyncNetPro.Sdk.Configuration;

namespace SyncNetPro.Hsm;

/// <summary>Klien SyncNetHsm (<c>HsmService</c> SDK lama).</summary>
public interface IHsmClient
{
    /// <summary>Generate working key node (<c>HsmService.GenerateKey</c>).</summary>
    Task<HsmKeyResult> GenerateNodeKeyAsync(string node, CancellationToken cancellationToken = default);

    /// <summary>Generate key terminal (<c>HsmService.GenerateKeyTerminal</c>).</summary>
    Task<HsmKeyResult> GenerateTerminalKeyAsync(string terminalId, CancellationToken cancellationToken = default);

    /// <summary>Simpan ZPK baru dari remote (key exchange) — <c>HsmService.UpdateKey</c>.</summary>
    Task<HsmKeyResult> TranslateKeyAsync(string node, string keyUnderZmk, CancellationToken cancellationToken = default);

    /// <summary>Translate PIN block dari node asal ke node tujuan.</summary>
    /// <param name="sourceNode">Node asal.</param>
    /// <param name="destinationNode">Node tujuan.</param>
    /// <param name="pinBlock">PIN block terenkripsi (hex).</param>
    /// <param name="pan">PAN; nomor rekening (12 digit kanan tanpa check digit) diambil otomatis.</param>
    /// <param name="cancellationToken">Pembatalan.</param>
    Task<HsmPinResult> TranslatePinBlockAsync(string sourceNode, string destinationNode, string pinBlock, string? pan, CancellationToken cancellationToken = default);

    /// <summary>Translate PIN block dari terminal (TPK) ke node tujuan.</summary>
    Task<HsmPinResult> TranslateTerminalPinBlockAsync(string terminalId, string destinationNode, string pinBlock, string? pan, CancellationToken cancellationToken = default);
}

internal sealed class HsmClient(HttpClient http, IOptions<HsmOptions> options, CoreEnvironment core, ILogger<HsmClient> logger) : IHsmClient
{
    private static readonly JsonSerializerSettings Json = new() { Formatting = Formatting.None };

    public Task<HsmKeyResult> GenerateNodeKeyAsync(string node, CancellationToken cancellationToken = default) =>
        PostAsync<HsmKeyResult>(HsmPaths.GenerateKey, new HsmRequests.GenerateKey(node, null), cancellationToken);

    public Task<HsmKeyResult> GenerateTerminalKeyAsync(string terminalId, CancellationToken cancellationToken = default) =>
        PostAsync<HsmKeyResult>(HsmPaths.GenerateKeyTerminal, new HsmRequests.GenerateKey(null, terminalId), cancellationToken);

    public Task<HsmKeyResult> TranslateKeyAsync(string node, string keyUnderZmk, CancellationToken cancellationToken = default) =>
        PostAsync<HsmKeyResult>(HsmPaths.TranslateKey, new HsmRequests.UpdateKey(node, keyUnderZmk), cancellationToken);

    public Task<HsmPinResult> TranslatePinBlockAsync(string sourceNode, string destinationNode, string pinBlock, string? pan, CancellationToken cancellationToken = default) =>
        PostAsync<HsmPinResult>(HsmPaths.TranslatePinBlock, new HsmRequests.TranslatePinBlock(sourceNode, destinationNode, pinBlock, AccountNumber(pan)), cancellationToken);

    public Task<HsmPinResult> TranslateTerminalPinBlockAsync(string terminalId, string destinationNode, string pinBlock, string? pan, CancellationToken cancellationToken = default) =>
        PostAsync<HsmPinResult>(HsmPaths.TranslatePinBlockTerminal, new HsmRequests.TranslateTerminalPinBlock(terminalId, destinationNode, pinBlock, AccountNumber(pan)), cancellationToken);

    /// <summary>12 digit PAN paling kanan tanpa check digit; kosong bila PAN ≤ 12 digit (sama dengan SDK lama).</summary>
    internal static string AccountNumber(string? pan) => pan is { Length: > 12 } ? pan.Substring(pan.Length - 13, 12) : string.Empty;

    internal static string Serialize(object request) => JsonConvert.SerializeObject(request, Json);

    private async Task<T> PostAsync<T>(string path, object request, CancellationToken cancellationToken)
        where T : HsmResult, new()
    {
        string url = BaseUrl() + path;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.Value.Timeout);
        try
        {
            using var content = new StringContent(Serialize(request), Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await http.PostAsync(new Uri(url), content, timeout.Token).ConfigureAwait(false);
            string body = await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return Failed<T>(path, $"HTTP {(int)response.StatusCode}");
            }

            return string.IsNullOrWhiteSpace(body)
                ? Failed<T>(path, "response kosong")
                : JsonConvert.DeserializeObject<T>(body, Json) ?? Failed<T>(path, "response kosong");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Failed<T>(path, $"timeout {options.Value.Timeout.TotalSeconds:0.#} detik");
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            return Failed<T>(path, ex.Message);
        }
    }

    private string BaseUrl()
    {
        string? url = options.Value.Url ?? core.HsmUrl;
        return string.IsNullOrWhiteSpace(url)
            ? throw new InvalidOperationException("URL SyncNetHsm belum diatur: isi SyncNet:Hsm:Url atau Hsm:Url di config Core.")
            : url.TrimEnd('/');
    }

    private T Failed<T>(string path, string reason)
        where T : HsmResult, new()
    {
        logger.LogWarning("SyncNetHsm {Path} gagal: {Reason}", path, reason);
        return new T { ResponseCode = HsmResult.Failed, ResponseMessage = reason };
    }
}

/// <summary>Registrasi klien HSM.</summary>
public static class HsmServiceCollectionExtensions
{
    /// <summary>
    /// Mendaftarkan <see cref="IHsmClient"/>. URL dari <c>SyncNet:Hsm:Url</c>, atau <c>Hsm:Url</c> config Core bila kosong.
    /// </summary>
    public static IHostApplicationBuilder AddSyncNetHsm(this IHostApplicationBuilder builder, Action<HsmOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddSyncNetHsm(configure);
        return builder;
    }

    /// <inheritdoc cref="AddSyncNetHsm(IHostApplicationBuilder, Action{HsmOptions}?)"/>
    public static IServiceCollection AddSyncNetHsm(this IServiceCollection services, Action<HsmOptions>? configure = null)
    {
        OptionsBuilder<HsmOptions> options = services.AddOptions<HsmOptions>().BindConfiguration(HsmOptions.SectionName);
        if (configure is not null) options.Configure(configure);
        options.Validate(o => o.Timeout > TimeSpan.Zero, "SyncNet:Hsm:Timeout harus > 0.");

        services.AddHttpClient<IHsmClient, HsmClient>(c => c.Timeout = Timeout.InfiniteTimeSpan);
        return services;
    }
}
