using System.Net.Http.Json;
using SyncNetApi.Dtos.Tools;

namespace SyncNetWasm.Services
{
    /// <summary>Calls menu "Tools" endpoints (ToolsController) — 4 stateless calculators/
    /// decoders, nothing persisted.</summary>
    public class ToolsService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ToolsService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<(string? Result, string? Error)> DesEncryptAsync(string value, string key, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/tools/des/encrypt", new DesCalculatorRequest { Value = value, Key = key }, ct);
            if (!response.IsSuccessStatusCode)
                return (null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            var result = await response.Content.ReadFromJsonAsync<ToolResultDto>(cancellationToken: ct);
            return (result?.Result, null);
        }

        public async Task<(string? Result, string? Error)> DesDecryptAsync(string value, string key, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/tools/des/decrypt", new DesCalculatorRequest { Value = value, Key = key }, ct);
            if (!response.IsSuccessStatusCode)
                return (null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            var result = await response.Content.ReadFromJsonAsync<ToolResultDto>(cancellationToken: ct);
            return (result?.Result, null);
        }

        public async Task<(string? Result, string? Error)> CalculatePinBlockAsync(string format, string pan, string pin, string key, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/tools/pinblock", new PinblockCalculatorRequest { Format = format, Pan = pan, Pin = pin, Key = key }, ct);
            if (!response.IsSuccessStatusCode)
                return (null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            var result = await response.Content.ReadFromJsonAsync<ToolResultDto>(cancellationToken: ct);
            return (result?.Result, null);
        }

        public async Task<(List<IccTlvEntryDto>? Result, string? Error)> DecodeIccDataAsync(string value, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/tools/icc-data-decode", new IccDataDecodeRequest { Value = value }, ct);
            if (!response.IsSuccessStatusCode)
                return (null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (await response.Content.ReadFromJsonAsync<List<IccTlvEntryDto>>(cancellationToken: ct), null);
        }

        public async Task<(string? Result, string? Error)> EncryptCredentialAsync(string value, string configType, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/tools/encrypt-credential", new EncryptCredentialRequest { Value = value, ConfigType = configType }, ct);
            if (!response.IsSuccessStatusCode)
                return (null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            var result = await response.Content.ReadFromJsonAsync<ToolResultDto>(cancellationToken: ct);
            return (result?.Result, null);
        }
    }
}
