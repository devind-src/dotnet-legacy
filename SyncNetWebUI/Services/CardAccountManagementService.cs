using System.Net.Http.Json;
using SyncNetApi.Dtos.CardAccounts;

namespace SyncNetWasm.Services
{
    public class CardAccountManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CardAccountManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<CardAccountDto>?> GetAccountsAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/card-accounts" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<CardAccountDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, CardAccountDto? Account, string? Error)> CreateAccountAsync(CreateCardAccountRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/card-accounts", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<CardAccountDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateAccountAsync(int id, UpdateCardAccountRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/card-accounts/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteAccountAsync(int id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/card-accounts/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
