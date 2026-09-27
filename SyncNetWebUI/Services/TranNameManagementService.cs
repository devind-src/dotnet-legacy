using System.Net.Http.Json;
using SyncNetApi.Dtos.TranNames;

namespace SyncNetWasm.Services
{
    public class TranNameManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public TranNameManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<TranNameDto>?> GetTranNamesAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/tran-names" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<TranNameDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, TranNameDto? TranName, string? Error)> CreateTranNameAsync(CreateTranNameRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/tran-names", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<TranNameDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateTranNameAsync(string transCode, UpdateTranNameRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/tran-names/{Uri.EscapeDataString(transCode)}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteTranNameAsync(string transCode, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/tran-names/{Uri.EscapeDataString(transCode)}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
