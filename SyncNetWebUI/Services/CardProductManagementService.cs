using System.Net.Http.Json;
using SyncNetApi.Dtos.CardProducts;

namespace SyncNetWasm.Services
{
    public class CardProductManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CardProductManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<CardProductDto>?> GetProductsAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/card-products" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<CardProductDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, CardProductDto? Product, string? Error)> CreateProductAsync(CreateCardProductRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/card-products", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<CardProductDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateProductAsync(int id, UpdateCardProductRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/card-products/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteProductAsync(int id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/card-products/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
