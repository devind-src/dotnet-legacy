using System.Net.Http.Json;
using SyncNetApi.Dtos.ProductFeeTierings;

namespace SyncNetWasm.Services
{
    public class ProductFeeTieringManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductFeeTieringManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<ProductFeeTieringDto>?> GetTieringsAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/product-fee-tierings" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<ProductFeeTieringDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, ProductFeeTieringDto? Item, string? Error)> CreateTieringAsync(CreateProductFeeTieringRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/product-fee-tierings", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<ProductFeeTieringDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateTieringAsync(long id, UpdateProductFeeTieringRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/product-fee-tierings/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteTieringAsync(long id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/product-fee-tierings/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
