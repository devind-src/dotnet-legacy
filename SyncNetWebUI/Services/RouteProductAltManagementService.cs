using System.Net.Http.Json;
using SyncNetApi.Dtos.RouteProductAlts;

namespace SyncNetWasm.Services
{
    public class RouteProductAltManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public RouteProductAltManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<RouteProductAltDto>?> GetAlternatesAsync(string instId, CancellationToken ct = default)
        {
            var response = await Client.GetAsync($"api/v1/route-product-alts?instId={Uri.EscapeDataString(instId)}", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<RouteProductAltDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, string? Error)> CreateAsync(CreateRouteProductAltRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/route-product-alts", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateRouteProductAltRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/route-product-alts/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/route-product-alts/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
