using System.Net.Http.Json;
using SyncNetApi.Dtos.RouteBins;

namespace SyncNetWasm.Services
{
    public class RouteBinManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public RouteBinManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<RouteBinDto>?> GetRoutesAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/route-bins" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<RouteBinDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, RouteBinDto? Route, string? Error)> CreateRouteAsync(CreateRouteBinRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/route-bins", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<RouteBinDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateRouteAsync(int groupId, UpdateRouteBinRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/route-bins/{groupId}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteRouteAsync(int groupId, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/route-bins/{groupId}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
