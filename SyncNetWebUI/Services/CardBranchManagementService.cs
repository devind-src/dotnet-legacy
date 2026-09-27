using System.Net.Http.Json;
using SyncNetApi.Dtos.CardBranches;

namespace SyncNetWasm.Services
{
    public class CardBranchManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CardBranchManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<CardBranchDto>?> GetByIssuerAsync(string issuer, CancellationToken ct = default)
        {
            var response = await Client.GetAsync($"api/v1/card-branches?issuer={Uri.EscapeDataString(issuer)}", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<CardBranchDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, CardBranchDto? Branch, string? Error)> CreateBranchAsync(CreateCardBranchRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/card-branches", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<CardBranchDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateBranchAsync(int id, UpdateCardBranchRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/card-branches/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteBranchAsync(int id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/card-branches/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
