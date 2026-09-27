using System.Net.Http.Json;
using SyncNetApi.Dtos.AgentBanks;

namespace SyncNetWasm.Services
{
    public class AgentBankManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AgentBankManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<AgentBankDto>?> GetRecordsAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/agent-banks" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<AgentBankDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, AgentBankDto? Item, string? Error)> CreateAsync(CreateAgentBankRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/agent-banks", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<AgentBankDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(long id, UpdateAgentBankRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/agent-banks/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteAsync(long id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/agent-banks/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
