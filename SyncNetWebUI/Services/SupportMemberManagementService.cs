using System.Net.Http.Json;
using SyncNetApi.Dtos.SupportMembers;

namespace SyncNetWasm.Services
{
    public class SupportMemberManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public SupportMemberManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<SupportMemberDto>?> GetMembersAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/support-members" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<SupportMemberDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, SupportMemberDto? Member, string? Error)> CreateMemberAsync(CreateSupportMemberRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/support-members", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<SupportMemberDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateMemberAsync(int memberId, UpdateSupportMemberRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/support-members/{memberId}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteMemberAsync(int memberId, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/support-members/{memberId}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
