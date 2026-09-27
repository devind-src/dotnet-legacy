using System.Net.Http.Json;
using SyncNetApi.Dtos.ProductPostpaidBillers;

namespace SyncNetWasm.Services
{
    /// <summary>Detail biller Product &gt; Pricing &amp; Fees &gt; Postpaid Fees (api/v1/product/pricing-fees/postpaid-billers).</summary>
    public class ProductPostpaidBillerManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductPostpaidBillerManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<ProductPostpaidBillersDto?> GetAsync(string productId, CancellationToken ct = default)
        {
            var response = await Client.GetAsync($"api/v1/product/pricing-fees/postpaid-billers/{Uri.EscapeDataString(productId)}", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<ProductPostpaidBillersDto>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, string? Error)> CreateAsync(CreateProductPostpaidBillerRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/product/pricing-fees/postpaid-billers", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(string productId, int nodeId, UpdateProductPostpaidBillerRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/product/pricing-fees/postpaid-billers/{Uri.EscapeDataString(productId)}/{nodeId}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> UpdateWeightsAsync(string productId, UpdateProductPostpaidBillerWeightsRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/product/pricing-fees/postpaid-billers/{Uri.EscapeDataString(productId)}/weights", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteAsync(string productId, int nodeId, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/product/pricing-fees/postpaid-billers/{Uri.EscapeDataString(productId)}/{nodeId}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
