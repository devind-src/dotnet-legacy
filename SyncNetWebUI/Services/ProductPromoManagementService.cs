using System.Net.Http.Json;
using SyncNetApi.Dtos.ProductPromos;

namespace SyncNetWasm.Services
{
    public class ProductPromoManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductPromoManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<ProductPromoDto>?> GetPromosAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/product-promos" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<ProductPromoDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, ProductPromoDto? Item, string? Error)> CreatePromoAsync(CreateProductPromoRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/product-promos", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<ProductPromoDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdatePromoAsync(long id, UpdateProductPromoRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/product-promos/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeletePromoAsync(long id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/product-promos/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
