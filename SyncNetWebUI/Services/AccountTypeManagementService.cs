using System.Net.Http.Json;
using SyncNetApi.Dtos.AccountTypes;

namespace SyncNetWasm.Services
{
    public class AccountTypeManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AccountTypeManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<AccountTypeDto>?> GetAccountTypesAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/account-types" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<AccountTypeDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, AccountTypeDto? AccountType, string? Error)> CreateAccountTypeAsync(CreateAccountTypeRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/account-types", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<AccountTypeDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateAccountTypeAsync(string acctType, UpdateAccountTypeRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/account-types/{Uri.EscapeDataString(acctType)}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteAccountTypeAsync(string acctType, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/account-types/{Uri.EscapeDataString(acctType)}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
