using System.Net.Http.Json;
using SyncNetApi.Dtos.SupportTeams;

namespace SyncNetWasm.Services
{
    public class SupportTeamManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public SupportTeamManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<SupportTeamDto>?> GetTeamsAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/support-teams" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<SupportTeamDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, SupportTeamDto? Team, string? Error)> CreateTeamAsync(CreateSupportTeamRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/support-teams", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<SupportTeamDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateTeamAsync(int teamId, UpdateSupportTeamRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/support-teams/{teamId}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteTeamAsync(int teamId, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/support-teams/{teamId}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
