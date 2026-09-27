using System.Net.Http.Json;
using SyncNetApi.Dtos.Cities;

namespace SyncNetWasm.Services
{
    public class CityManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CityManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<CityDto>?> GetCitiesAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/cities" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<CityDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, CityDto? City, string? Error)> CreateCityAsync(CreateCityRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/cities", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<CityDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateCityAsync(long id, UpdateCityRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/cities/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteCityAsync(long id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/cities/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
