using System.Net.Http.Json;
using SyncNetApi.Dtos.Brands;

namespace SyncNetWasm.Services
{
    public class BrandManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public BrandManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<BrandDto>?> GetBrandsAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/brands" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<BrandDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, BrandDto? Brand, string? Error)> CreateBrandAsync(CreateBrandRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/brands", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<BrandDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateBrandAsync(int id, UpdateBrandRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/brands/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteBrandAsync(int id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/brands/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
