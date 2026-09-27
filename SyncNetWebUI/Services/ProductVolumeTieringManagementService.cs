using System.Net.Http.Json;
using SyncNetApi.Dtos.ProductVolumeTierings;

namespace SyncNetWasm.Services
{
    public class ProductVolumeTieringManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductVolumeTieringManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<ProductVolumeTieringDto>?> GetTieringsAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/product/pricing-fees/volume-tiering" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<ProductVolumeTieringDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, ProductVolumeTieringDto? Item, string? Error)> CreateTieringAsync(CreateProductVolumeTieringRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/product/pricing-fees/volume-tiering", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<ProductVolumeTieringDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateTieringAsync(long id, UpdateProductVolumeTieringRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/product/pricing-fees/volume-tiering/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteTieringAsync(long id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/product/pricing-fees/volume-tiering/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
