using System.Net.Http.Json;
using SyncNetApi.Dtos.Merchants;

namespace SyncNetWasm.Services
{
    /// <summary>CRUD against api/v1/merchants. Mirrors RoleManagementService's pattern.</summary>
    public class MerchantManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public MerchantManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<MerchantDto>?> GetMerchantsAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = string.IsNullOrWhiteSpace(filter) ? "api/v1/merchants" : $"api/v1/merchants?filter={Uri.EscapeDataString(filter)}";
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<MerchantDto>>(cancellationToken: ct) : null;
        }

        public async Task<MerchantDto?> GetMerchantAsync(string merchantId, CancellationToken ct = default)
        {
            var response = await Client.GetAsync($"api/v1/merchants/{Uri.EscapeDataString(merchantId)}", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<MerchantDto>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, MerchantDto? Merchant, string? Error)> CreateMerchantAsync(CreateMerchantRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/merchants", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<MerchantDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateMerchantAsync(string merchantId, UpdateMerchantRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/merchants/{Uri.EscapeDataString(merchantId)}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteMerchantAsync(string merchantId, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/merchants/{Uri.EscapeDataString(merchantId)}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
