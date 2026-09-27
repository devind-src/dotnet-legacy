using System.Net.Http.Json;
using SyncNetApi.Dtos.ProductMappings;

namespace SyncNetWasm.Services
{
    public class ProductMappingManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductMappingManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<ProductMappingDto>?> GetMappingsAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/product/master/mappings" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<ProductMappingDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, ProductMappingDto? Mapping, string? Error)> CreateMappingAsync(CreateProductMappingRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/product/master/mappings", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<ProductMappingDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateMappingAsync(long id, UpdateProductMappingRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/product/master/mappings/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteMappingAsync(long id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/product/master/mappings/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
