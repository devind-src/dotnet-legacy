using System.Net.Http.Json;
using SyncNetApi.Dtos.MerchantGroups;

namespace SyncNetWasm.Services
{
    public class MerchantGroupManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public MerchantGroupManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<MerchantGroupDto>?> GetGroupsAsync(string? filter = null, string? participantId = null, CancellationToken ct = default)
        {
            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(filter)) query.Add($"filter={Uri.EscapeDataString(filter)}");
            if (!string.IsNullOrWhiteSpace(participantId)) query.Add($"participantId={Uri.EscapeDataString(participantId)}");
            var url = "api/v1/merchant-groups" + (query.Count > 0 ? "?" + string.Join("&", query) : "");

            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<MerchantGroupDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, MerchantGroupDto? Group, string? Error)> CreateGroupAsync(CreateMerchantGroupRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/merchant-groups", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<MerchantGroupDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateGroupAsync(string groupName, UpdateMerchantGroupRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/merchant-groups/{Uri.EscapeDataString(groupName)}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteGroupAsync(string groupName, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/merchant-groups/{Uri.EscapeDataString(groupName)}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
