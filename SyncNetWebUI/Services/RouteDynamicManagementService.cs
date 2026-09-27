using System.Net.Http.Json;
using SyncNetApi.Dtos.RouteDynamics;

namespace SyncNetWasm.Services
{
    public class RouteDynamicManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public RouteDynamicManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<RouteDynamicDto>?> GetRoutesAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/route-dynamics" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<RouteDynamicDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, RouteDynamicDto? Route, string? Error)> CreateRouteAsync(CreateRouteDynamicRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/route-dynamics", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<RouteDynamicDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateRouteAsync(string instId, UpdateRouteDynamicRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/route-dynamics/{Uri.EscapeDataString(instId)}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteRouteAsync(string instId, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/route-dynamics/{Uri.EscapeDataString(instId)}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
