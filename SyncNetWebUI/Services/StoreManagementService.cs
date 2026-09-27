using System.Net.Http.Json;
using SyncNetApi.Dtos.Stores;

namespace SyncNetWasm.Services
{
    public class StoreManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public StoreManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<StoreDto>?> GetStoresAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = string.IsNullOrWhiteSpace(filter) ? "api/v1/stores" : $"api/v1/stores?filter={Uri.EscapeDataString(filter)}";
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<StoreDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, StoreDto? Store, string? Error)> CreateStoreAsync(CreateStoreRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/stores", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<StoreDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateStoreAsync(string storeId, UpdateStoreRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/stores/{Uri.EscapeDataString(storeId)}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteStoreAsync(string storeId, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/stores/{Uri.EscapeDataString(storeId)}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
