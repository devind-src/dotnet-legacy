using System.Net.Http.Json;
using SyncNetApi.Dtos.ProductCategories;

namespace SyncNetWasm.Services
{
    public class ProductCategoryManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductCategoryManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<ProductCategoryDto>?> GetCategoriesAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/product/master/categories" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<ProductCategoryDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, ProductCategoryDto? Category, string? Error)> CreateCategoryAsync(CreateProductCategoryRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/product/master/categories", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<ProductCategoryDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateCategoryAsync(string category, UpdateProductCategoryRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/product/master/categories/{Uri.EscapeDataString(category)}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteCategoryAsync(string category, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/product/master/categories/{Uri.EscapeDataString(category)}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
