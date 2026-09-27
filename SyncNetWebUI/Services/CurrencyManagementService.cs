using System.Net.Http.Json;
using SyncNetApi.Dtos.Currencies;

namespace SyncNetWasm.Services
{
    public class CurrencyManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CurrencyManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<CurrencyDto>?> GetCurrenciesAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/currencies" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<CurrencyDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, CurrencyDto? Currency, string? Error)> CreateCurrencyAsync(CreateCurrencyRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/currencies", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<CurrencyDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateCurrencyAsync(string currencyCode, UpdateCurrencyRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/currencies/{Uri.EscapeDataString(currencyCode)}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteCurrencyAsync(string currencyCode, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/currencies/{Uri.EscapeDataString(currencyCode)}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
