using System.Net.Http.Json;
using SyncNetApi.Dtos.Terminals;

namespace SyncNetWasm.Services
{
    public class TerminalManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public TerminalManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<TerminalDto>?> GetTerminalsAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = string.IsNullOrWhiteSpace(filter) ? "api/v1/terminals" : $"api/v1/terminals?filter={Uri.EscapeDataString(filter)}";
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<TerminalDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, TerminalDto? Terminal, string? Error)> CreateTerminalAsync(CreateTerminalRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/terminals", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<TerminalDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateTerminalAsync(string termId, UpdateTerminalRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/terminals/{Uri.EscapeDataString(termId)}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteTerminalAsync(string termId, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/terminals/{Uri.EscapeDataString(termId)}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
