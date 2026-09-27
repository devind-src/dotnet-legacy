using System.Net.Http.Json;
using SyncNetApi.Dtos.Products;

namespace SyncNetWasm.Services
{
    public class ProductManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<ProductDto>?> GetProductsAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/product/master/products" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<ProductDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, ProductDto? Product, string? Error)> CreateProductAsync(CreateProductRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/product/master/products", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<ProductDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateProductAsync(string productCode, UpdateProductRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/product/master/products/{Uri.EscapeDataString(productCode)}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteProductAsync(string productCode, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/product/master/products/{Uri.EscapeDataString(productCode)}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
