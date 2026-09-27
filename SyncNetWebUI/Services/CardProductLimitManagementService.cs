using System.Net.Http.Json;
using SyncNetApi.Dtos.CardProductLimits;

namespace SyncNetWasm.Services
{
    public class CardProductLimitManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CardProductLimitManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<CardProductLimitDto>?> GetByProductIdAsync(int productId, CancellationToken ct = default)
        {
            var response = await Client.GetAsync($"api/v1/card-product-limits?productId={productId}", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<CardProductLimitDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, CardProductLimitDto? Limit, string? Error)> CreateLimitAsync(CreateCardProductLimitRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/card-product-limits", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<CardProductLimitDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateLimitAsync(int id, UpdateCardProductLimitRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/card-product-limits/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteLimitAsync(int id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/card-product-limits/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
