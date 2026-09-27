using System.Net.Http.Json;
using SyncNetApi.Dtos.Connections;

namespace SyncNetWasm.Services
{
    public class ConnectionManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ConnectionManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<ConnectionDto>?> GetByNodeIdAsync(int nodeId, CancellationToken ct = default)
        {
            var response = await Client.GetAsync($"api/v1/connections?nodeId={nodeId}", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<ConnectionDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, ConnectionDto? Connection, string? Error)> CreateConnectionAsync(CreateConnectionRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/connections", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<ConnectionDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateConnectionAsync(string connName, UpdateConnectionRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/connections/{Uri.EscapeDataString(connName)}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteConnectionAsync(string connName, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/connections/{Uri.EscapeDataString(connName)}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
