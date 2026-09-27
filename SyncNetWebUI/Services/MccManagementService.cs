using System.Net.Http.Json;
using SyncNetApi.Dtos.Mccs;

namespace SyncNetWasm.Services
{
    public class MccManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public MccManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<MccDto>?> GetMccsAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/mccs" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<MccDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, MccDto? Mcc, string? Error)> CreateMccAsync(CreateMccRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/mccs", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<MccDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateMccAsync(string mccCode, UpdateMccRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/mccs/{Uri.EscapeDataString(mccCode)}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteMccAsync(string mccCode, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/mccs/{Uri.EscapeDataString(mccCode)}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
