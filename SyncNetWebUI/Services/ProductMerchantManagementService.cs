using System.Net.Http.Json;
using SyncNetApi.Dtos.ProductMerchants;

namespace SyncNetWasm.Services
{
    public class ProductMerchantManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductMerchantManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<ProductMerchantDto>?> GetProductMerchantsAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/product/master/merchants" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<ProductMerchantDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, ProductMerchantDto? Item, string? Error)> CreateProductMerchantAsync(CreateProductMerchantRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/product/master/merchants", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<ProductMerchantDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateProductMerchantAsync(int id, UpdateProductMerchantRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/product/master/merchants/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteProductMerchantAsync(int id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/product/master/merchants/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
