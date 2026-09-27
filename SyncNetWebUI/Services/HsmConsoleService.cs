using System.Net.Http.Json;
using SyncNetApi.Dtos.HsmConsole;

namespace SyncNetWasm.Services
{
    /// <summary>Calls the HSM &gt; Console math endpoints (HsmConsoleController) — real
    /// DES/3DES key-ceremony math under LMK_1, ported from legacy NbHSM.</summary>
    public class HsmConsoleService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public HsmConsoleService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<(List<ComponentResultDto>? Result, string? Error)> GenerateComponentAsync(GenerateComponentRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/hsm-console/generate-component", request, ct);
            if (!response.IsSuccessStatusCode)
                return (null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (await response.Content.ReadFromJsonAsync<List<ComponentResultDto>>(cancellationToken: ct), null);
        }

        public async Task<(EncryptComponentResponseDto? Result, string? Error)> EncryptComponentAsync(string clearComponent, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/hsm-console/encrypt-component", new EncryptComponentRequest { ClearComponent = clearComponent }, ct);
            if (!response.IsSuccessStatusCode)
                return (null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (await response.Content.ReadFromJsonAsync<EncryptComponentResponseDto>(cancellationToken: ct), null);
        }

        public async Task<(string? Kcv, string? Error)> GetKeyCheckValueAsync(string component, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/hsm-console/key-check-value", new KeyCheckValueRequest { Component = component }, ct);
            if (!response.IsSuccessStatusCode)
                return (null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            var result = await response.Content.ReadFromJsonAsync<KeyCheckValueResponseDto>(cancellationToken: ct);
            return (result?.Kcv, null);
        }

        public async Task<(string? Kcv, string? Error)> GetComponentKcvAsync(string component, string inputMode, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/hsm-console/zmk/component-kcv", new ComponentKcvRequest { Component = component, InputMode = inputMode }, ct);
            if (!response.IsSuccessStatusCode)
                return (null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            var result = await response.Content.ReadFromJsonAsync<KeyCheckValueResponseDto>(cancellationToken: ct);
            return (result?.Kcv, null);
        }

        public async Task<(ZmkResultDto? Result, string? Error)> GenerateZmkAsync(List<string> components, string inputMode, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/hsm-console/zmk/generate", new FormZmkRequest { Components = components, InputMode = inputMode }, ct);
            if (!response.IsSuccessStatusCode)
                return (null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (await response.Content.ReadFromJsonAsync<ZmkResultDto>(cancellationToken: ct), null);
        }
    }
}
