using System.Net.Http.Json;
using SyncNetApi.Dtos.Banks;

namespace SyncNetWasm.Services
{
    public class BankManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public BankManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<BankDto>?> GetBanksAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/banks" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<BankDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, BankDto? Bank, string? Error)> CreateBankAsync(CreateBankRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/banks", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<BankDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateBankAsync(int id, UpdateBankRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/banks/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteBankAsync(int id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/banks/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
