using System.Net.Http.Json;
using SyncNetApi.Dtos.RouteMargins;

namespace SyncNetWasm.Services
{
    public class RouteMarginManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public RouteMarginManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<RouteMarginDto>?> GetRoutesAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/route-margins" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<RouteMarginDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, RouteMarginDto? Route, string? Error)> CreateRouteAsync(CreateRouteMarginRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/route-margins", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<RouteMarginDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateRouteAsync(string instId, UpdateRouteMarginRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/route-margins/{Uri.EscapeDataString(instId)}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteRouteAsync(string instId, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/route-margins/{Uri.EscapeDataString(instId)}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, List<RouteMarginDto>? Routes, string? Error)> SyncCategoryAsync(SyncRouteMarginRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync("api/v1/route-margins/sync", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<List<RouteMarginDto>>(cancellationToken: ct), null);
        }

    }
}
