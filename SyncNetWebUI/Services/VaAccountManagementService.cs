using System.Net.Http.Json;
using SyncNetApi.Dtos.VaAccounts;

namespace SyncNetWasm.Services
{
    public class VaAccountManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public VaAccountManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<VaAccountDto>?> GetAccountsAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/va-accounts" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<VaAccountDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, VaAccountDto? Account, string? Error)> CreateAccountAsync(CreateVaAccountRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/va-accounts", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<VaAccountDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateAccountAsync(string accNr, UpdateVaAccountRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/va-accounts/{Uri.EscapeDataString(accNr)}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteAccountAsync(string accNr, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/va-accounts/{Uri.EscapeDataString(accNr)}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
