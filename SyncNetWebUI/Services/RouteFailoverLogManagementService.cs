using System.Net.Http.Json;
using SyncNetApi.Dtos.RouteFailoverLogs;

namespace SyncNetWasm.Services
{
    public class RouteFailoverLogManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public RouteFailoverLogManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<RouteFailoverLogDto>?> GetLogsAsync(DateTime? from = null, DateTime? to = null,
            string? instId = null, string? supplierId = null, string? reason = null, string? routingType = null, CancellationToken ct = default)
        {
            var parts = new List<string>();
            if (from.HasValue) parts.Add($"from={Uri.EscapeDataString(from.Value.ToString("o"))}");
            if (to.HasValue) parts.Add($"to={Uri.EscapeDataString(to.Value.ToString("o"))}");
            if (!string.IsNullOrWhiteSpace(instId)) parts.Add($"instId={Uri.EscapeDataString(instId)}");
            if (!string.IsNullOrWhiteSpace(supplierId)) parts.Add($"supplierId={Uri.EscapeDataString(supplierId)}");
            if (!string.IsNullOrWhiteSpace(reason)) parts.Add($"reason={Uri.EscapeDataString(reason)}");
            if (!string.IsNullOrWhiteSpace(routingType)) parts.Add($"routingType={Uri.EscapeDataString(routingType)}");

            var url = "api/v1/route-failover-logs" + (parts.Count > 0 ? $"?{string.Join('&', parts)}" : "");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<RouteFailoverLogDto>>(cancellationToken: ct) : null;
        }
    }
}
