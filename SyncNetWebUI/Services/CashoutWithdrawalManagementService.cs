using System.Net.Http.Json;
using SyncNetApi.Dtos.CashoutWithdrawals;

namespace SyncNetWasm.Services
{
    public class CashoutWithdrawalManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CashoutWithdrawalManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<CashoutWithdrawalDto>?> GetRecordsAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/cashout-withdrawals" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<CashoutWithdrawalDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, CashoutWithdrawalDto? Item, string? Error)> CreateAsync(CreateCashoutWithdrawalRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/cashout-withdrawals", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<CashoutWithdrawalDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(long id, UpdateCashoutWithdrawalRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/cashout-withdrawals/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteAsync(long id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/cashout-withdrawals/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
