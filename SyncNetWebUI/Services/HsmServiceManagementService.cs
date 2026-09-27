using System.Net.Http.Json;
using SyncNetApi.Dtos.HsmServices;

namespace SyncNetWasm.Services
{
    public class HsmServiceManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public HsmServiceManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<HsmServiceDto>?> GetHsmServicesAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/hsm-services" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<HsmServiceDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, HsmServiceDto? Service, string? Error)> CreateHsmServiceAsync(CreateHsmServiceRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/hsm-services", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<HsmServiceDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateHsmServiceAsync(string hsmDesc, UpdateHsmServiceRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/hsm-services/{Uri.EscapeDataString(hsmDesc)}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteHsmServiceAsync(string hsmDesc, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/hsm-services/{Uri.EscapeDataString(hsmDesc)}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
