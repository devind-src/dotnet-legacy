using System.Net.Http.Json;
using SyncNetApi.Dtos.CashoutMiniAtms;

namespace SyncNetWasm.Services
{
    public class CashoutMiniAtmManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CashoutMiniAtmManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<CashoutMiniAtmDto>?> GetRecordsAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/cashout-mini-atms" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<CashoutMiniAtmDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, CashoutMiniAtmDto? Item, string? Error)> CreateAsync(CreateCashoutMiniAtmRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/cashout-mini-atms", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<CashoutMiniAtmDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(long id, UpdateCashoutMiniAtmRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/cashout-mini-atms/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteAsync(long id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/cashout-mini-atms/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
