using System.Net.Http.Json;
using SyncNetApi.Dtos.ProductMerchantPricing;

namespace SyncNetWasm.Services
{
    public class ProductMerchantPricingManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductMerchantPricingManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<ProductMerchantPricingDto>?> GetPricesAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/product/pricing-fees/merchant-pricing" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<ProductMerchantPricingDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, ProductMerchantPricingDto? Item, string? Error)> CreatePriceAsync(CreateProductMerchantPricingRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/product/pricing-fees/merchant-pricing", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<ProductMerchantPricingDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdatePriceAsync(int id, UpdateProductMerchantPricingRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/product/pricing-fees/merchant-pricing/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeletePriceAsync(int id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/product/pricing-fees/merchant-pricing/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
