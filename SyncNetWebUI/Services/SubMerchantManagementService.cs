using System.Net.Http.Json;
using SyncNetApi.Dtos.SubMerchants;

namespace SyncNetWasm.Services
{
    public class SubMerchantManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public SubMerchantManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<SubMerchantDto>?> GetSubMerchantsAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = string.IsNullOrWhiteSpace(filter) ? "api/v1/submerchants" : $"api/v1/submerchants?filter={Uri.EscapeDataString(filter)}";
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<SubMerchantDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, SubMerchantDto? SubMerchant, string? Error)> CreateSubMerchantAsync(CreateSubMerchantRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/submerchants", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<SubMerchantDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateSubMerchantAsync(string subMerchantId, UpdateSubMerchantRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/submerchants/{Uri.EscapeDataString(subMerchantId)}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteSubMerchantAsync(string subMerchantId, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/submerchants/{Uri.EscapeDataString(subMerchantId)}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
