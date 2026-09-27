using System.Net.Http.Json;
using SyncNetApi.Dtos.TerminalClients;

namespace SyncNetWasm.Services
{
    public class TerminalClientManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public TerminalClientManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<TerminalClientDto>?> GetClientsAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = string.IsNullOrWhiteSpace(filter) ? "api/v1/terminal-clients" : $"api/v1/terminal-clients?filter={Uri.EscapeDataString(filter)}";
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<TerminalClientDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, TerminalClientDto? Client, string? Error)> CreateClientAsync(CreateTerminalClientRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/terminal-clients", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<TerminalClientDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateClientAsync(string clientId, UpdateTerminalClientRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/terminal-clients/{Uri.EscapeDataString(clientId)}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteClientAsync(string clientId, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/terminal-clients/{Uri.EscapeDataString(clientId)}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
