using System.Net.Http.Json;
using SyncNetApi.Dtos.SubMerchantGroups;

namespace SyncNetWasm.Services
{
    public class SubMerchantGroupManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public SubMerchantGroupManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<SubMerchantGroupDto>?> GetGroupsAsync(string? filter = null, string? participantId = null, CancellationToken ct = default)
        {
            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(filter)) query.Add($"filter={Uri.EscapeDataString(filter)}");
            if (!string.IsNullOrWhiteSpace(participantId)) query.Add($"participantId={Uri.EscapeDataString(participantId)}");
            var url = "api/v1/submerchant-groups" + (query.Count > 0 ? "?" + string.Join("&", query) : "");

            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<SubMerchantGroupDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, SubMerchantGroupDto? Group, string? Error)> CreateGroupAsync(CreateSubMerchantGroupRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/submerchant-groups", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<SubMerchantGroupDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateGroupAsync(string groupName, UpdateSubMerchantGroupRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/submerchant-groups/{Uri.EscapeDataString(groupName)}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteGroupAsync(string groupName, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/submerchant-groups/{Uri.EscapeDataString(groupName)}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
