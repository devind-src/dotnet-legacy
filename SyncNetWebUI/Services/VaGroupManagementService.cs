using System.Net.Http.Json;
using SyncNetApi.Dtos.VaGroups;

namespace SyncNetWasm.Services
{
    public class VaGroupManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public VaGroupManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<VaGroupDto>?> GetGroupsAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/va-groups" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<VaGroupDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, VaGroupDto? Group, string? Error)> CreateGroupAsync(CreateVaGroupRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/va-groups", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<VaGroupDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateGroupAsync(string groupName, UpdateVaGroupRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/va-groups/{Uri.EscapeDataString(groupName)}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteGroupAsync(string groupName, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/va-groups/{Uri.EscapeDataString(groupName)}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
