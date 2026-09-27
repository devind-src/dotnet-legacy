using System.Net.Http.Json;
using SyncNetApi.Dtos.Countries;

namespace SyncNetWasm.Services
{
    public class CountryManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CountryManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<CountryDto>?> GetCountriesAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/countries" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<CountryDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, CountryDto? Country, string? Error)> CreateCountryAsync(CreateCountryRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/countries", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<CountryDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateCountryAsync(string name, UpdateCountryRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/countries/{Uri.EscapeDataString(name)}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteCountryAsync(string name, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/countries/{Uri.EscapeDataString(name)}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
