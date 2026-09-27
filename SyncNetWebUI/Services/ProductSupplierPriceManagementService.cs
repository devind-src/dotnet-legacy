using System.Net.Http.Json;
using SyncNetApi.Dtos.ProductSupplierPrices;

namespace SyncNetWasm.Services
{
    public class ProductSupplierPriceManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductSupplierPriceManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<ProductSupplierPriceDto>?> GetPricesAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/product-supplier-prices" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<ProductSupplierPriceDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, ProductSupplierPriceDto? Item, string? Error)> CreatePriceAsync(CreateProductSupplierPriceRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/product-supplier-prices", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<ProductSupplierPriceDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdatePriceAsync(int id, UpdateProductSupplierPriceRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/product-supplier-prices/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeletePriceAsync(int id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/product-supplier-prices/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
