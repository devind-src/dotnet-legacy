using System.Net.Http.Json;
using SyncNetApi.Dtos.TerminalLimits;

namespace SyncNetWasm.Services
{
    public class TerminalLimitManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public TerminalLimitManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<TerminalLimitDto>?> GetLimitsAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = string.IsNullOrWhiteSpace(filter) ? "api/v1/terminal-limits" : $"api/v1/terminal-limits?filter={Uri.EscapeDataString(filter)}";
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<TerminalLimitDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, TerminalLimitDto? Limit, string? Error)> CreateLimitAsync(CreateTerminalLimitRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/terminal-limits", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<TerminalLimitDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateLimitAsync(long id, UpdateTerminalLimitRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/terminal-limits/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteLimitAsync(long id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/terminal-limits/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
