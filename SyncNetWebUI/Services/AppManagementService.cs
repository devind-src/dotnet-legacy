using System.Net.Http.Json;
using SyncNetApi.Dtos.Apps;

namespace SyncNetWasm.Services
{
    public class AppManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AppManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<AppDto>?> GetAppsAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/apps" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<AppDto>>(cancellationToken: ct) : null;
        }

        public async Task<int?> GetNewCommandPortAsync(CancellationToken ct = default)
        {
            var response = await Client.GetAsync("api/v1/apps/new-command-port", ct);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<int>(cancellationToken: ct);
        }

        public async Task<(bool Success, AppDto? App, string? Error)> CreateAppAsync(CreateAppRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/apps", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<AppDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateAppAsync(string appName, UpdateAppRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/apps/{Uri.EscapeDataString(appName)}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteAppAsync(string appName, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/apps/{Uri.EscapeDataString(appName)}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
