using System.Net.Http.Json;
using SyncNetApi.Dtos.RouteSupplierStatuses;

namespace SyncNetWasm.Services
{
    public class RouteSupplierStatusManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public RouteSupplierStatusManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<RouteSupplierStatusDto>?> GetStatusesAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/route-supplier-statuses" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<RouteSupplierStatusDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, string? Error)> ResetAsync(string supplierId, CancellationToken ct = default)
        {
            var response = await Client.PostAsync($"api/v1/route-supplier-statuses/{Uri.EscapeDataString(supplierId)}/reset", null, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
