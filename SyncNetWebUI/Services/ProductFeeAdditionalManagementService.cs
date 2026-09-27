using System.Net.Http.Json;
using SyncNetApi.Dtos.ProductFeeAdditionals;

namespace SyncNetWasm.Services
{
    public class ProductFeeAdditionalManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductFeeAdditionalManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<ProductFeeAdditionalDto>?> GetFeesAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/product-fee-additionals" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<ProductFeeAdditionalDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, ProductFeeAdditionalDto? Item, string? Error)> CreateFeeAsync(CreateProductFeeAdditionalRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/product-fee-additionals", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<ProductFeeAdditionalDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateFeeAsync(long id, UpdateProductFeeAdditionalRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/product-fee-additionals/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteFeeAsync(long id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/product-fee-additionals/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
