using System.Net.Http.Json;
using SyncNetApi.Dtos.ProductPrepaidPricing;

namespace SyncNetWasm.Services
{
    public class ProductPrepaidPricingManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductPrepaidPricingManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<ProductPrepaidPricingDto>?> GetPricesAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/product/pricing-fees/prepaid-pricing" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<ProductPrepaidPricingDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, ProductPrepaidPricingDto? Item, string? Error)> CreatePriceAsync(CreateProductPrepaidPricingRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/product/pricing-fees/prepaid-pricing", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<ProductPrepaidPricingDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdatePriceAsync(int id, UpdateProductPrepaidPricingRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/product/pricing-fees/prepaid-pricing/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<List<TopupRoutingDto>?> GetProductsAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/product/pricing-fees/prepaid-pricing/products" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<TopupRoutingDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, string? Error)> UpdateRoutingAsync(string productId, UpdateTopupRoutingRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/product/pricing-fees/prepaid-pricing/products/{Uri.EscapeDataString(productId)}/routing", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> UpdateWeightsAsync(string productId, UpdateSupplierWeightsRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/product/pricing-fees/prepaid-pricing/products/{Uri.EscapeDataString(productId)}/weights", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> ApplyToAllDenomsAsync(int id, CancellationToken ct = default)
        {
            var response = await Client.PostAsync($"api/v1/product/pricing-fees/prepaid-pricing/{id}/apply-all-denoms", null, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeletePriceAsync(int id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/product/pricing-fees/prepaid-pricing/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
