using System.Net.Http.Json;
using SyncNetApi.Dtos.ProductMerchantPrices;

namespace SyncNetWasm.Services
{
    public class ProductMerchantPriceManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductMerchantPriceManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<ProductMerchantPriceDto>?> GetPricesAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/product-merchant-prices" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<ProductMerchantPriceDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, ProductMerchantPriceDto? Item, string? Error)> CreatePriceAsync(CreateProductMerchantPriceRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/product-merchant-prices", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<ProductMerchantPriceDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdatePriceAsync(int id, UpdateProductMerchantPriceRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/product-merchant-prices/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeletePriceAsync(int id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/product-merchant-prices/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
