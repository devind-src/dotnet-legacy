using System.Net.Http.Json;
using SyncNetApi.Dtos.CardGroups;

namespace SyncNetWasm.Services
{
    public class CardGroupManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CardGroupManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<CardGroupDto>?> GetGroupsAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/card-groups" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<CardGroupDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, CardGroupDto? Group, string? Error)> CreateGroupAsync(CreateCardGroupRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/card-groups", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<CardGroupDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateGroupAsync(int groupId, UpdateCardGroupRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/card-groups/{groupId}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteGroupAsync(int groupId, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/card-groups/{groupId}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
