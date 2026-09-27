using System.Net.Http.Json;
using SyncNetApi.Dtos.CardOverrideLimits;

namespace SyncNetWasm.Services
{
    public class CardOverrideLimitManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CardOverrideLimitManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<CardOverrideLimitDto>?> GetLimitsAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/card-override-limits" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<CardOverrideLimitDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, CardOverrideLimitDto? Limit, string? Error)> CreateLimitAsync(CreateCardOverrideLimitRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/card-override-limits", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<CardOverrideLimitDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateLimitAsync(int id, UpdateCardOverrideLimitRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/card-override-limits/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteLimitAsync(int id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/card-override-limits/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
