using System.Net.Http.Json;
using SyncNetApi.Dtos.RouteSources;

namespace SyncNetWasm.Services
{
    public class RouteSourceManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public RouteSourceManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<RouteSourceDto>?> GetRoutesAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/route-sources" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<RouteSourceDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, RouteSourceDto? Route, string? Error)> CreateRouteAsync(CreateRouteSourceRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/route-sources", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<RouteSourceDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateRouteAsync(int id, UpdateRouteSourceRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/route-sources/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteRouteAsync(int id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/route-sources/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
