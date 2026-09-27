using System.Net.Http.Json;
using SyncNetApi.Dtos.MerchantCriteria;

namespace SyncNetWasm.Services
{
    public class MerchantCriteriaManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public MerchantCriteriaManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<MerchantCriteriaDto>?> GetCriteriaAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/merchant-criteria" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<MerchantCriteriaDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, MerchantCriteriaDto? Item, string? Error)> CreateCriteriaAsync(CreateMerchantCriteriaRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/merchant-criteria", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<MerchantCriteriaDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateCriteriaAsync(long id, UpdateMerchantCriteriaRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/merchant-criteria/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteCriteriaAsync(long id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/merchant-criteria/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
