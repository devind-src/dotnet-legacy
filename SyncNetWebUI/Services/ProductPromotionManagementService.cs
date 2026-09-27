using System.Net.Http.Json;
using SyncNetApi.Dtos.ProductPromotions;

namespace SyncNetWasm.Services
{
    public class ProductPromotionManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductPromotionManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<ProductPromotionDto>?> GetPromosAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/product/pricing-fees/promotions" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<ProductPromotionDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, ProductPromotionDto? Item, string? Error)> CreatePromoAsync(CreateProductPromotionRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/product/pricing-fees/promotions", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<ProductPromotionDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdatePromoAsync(long id, UpdateProductPromotionRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/product/pricing-fees/promotions/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeletePromoAsync(long id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/product/pricing-fees/promotions/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
