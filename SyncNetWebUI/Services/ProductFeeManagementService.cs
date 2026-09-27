using System.Net.Http.Json;
using SyncNetApi.Dtos.ProductFees;

namespace SyncNetWasm.Services
{
    public class ProductFeeManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductFeeManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<ProductFeeDto>?> GetFeesAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/product-fees" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<ProductFeeDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, ProductFeeDto? Item, string? Error)> CreateFeeAsync(CreateProductFeeRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/product-fees", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<ProductFeeDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateFeeAsync(long id, UpdateProductFeeRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/product-fees/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteFeeAsync(long id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/product-fees/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
