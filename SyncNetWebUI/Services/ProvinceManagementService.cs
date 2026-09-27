using System.Net.Http.Json;
using SyncNetApi.Dtos.Provinces;

namespace SyncNetWasm.Services
{
    public class ProvinceManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProvinceManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<ProvinceDto>?> GetProvincesAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/provinces" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<ProvinceDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, ProvinceDto? Province, string? Error)> CreateProvinceAsync(CreateProvinceRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/provinces", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<ProvinceDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateProvinceAsync(long id, UpdateProvinceRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/provinces/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteProvinceAsync(long id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/provinces/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
