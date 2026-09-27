using System.Net.Http.Json;
using SyncNetApi.Dtos.RouteFailoverConfigs;

namespace SyncNetWasm.Services
{
    public class RouteFailoverConfigManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public RouteFailoverConfigManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<RouteFailoverConfigDto>?> GetConfigsAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/route-failover-configs" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<RouteFailoverConfigDto>>(cancellationToken: ct) : null;
        }

        public async Task<List<FailoverSwitchDto>?> GetSwitchesAsync(CancellationToken ct = default)
        {
            var response = await Client.GetAsync("api/v1/route-failover-configs/switches", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<FailoverSwitchDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, string? Error)> SetSwitchAsync(string routingType, bool enabled, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/route-failover-configs/switches/{Uri.EscapeDataString(routingType)}",
                new SetFailoverSwitchRequest { Enabled = enabled }, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, RouteFailoverConfigDto? Config, string? Error)> CreateConfigAsync(CreateRouteFailoverConfigRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/route-failover-configs", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<RouteFailoverConfigDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateConfigAsync(int id, UpdateRouteFailoverConfigRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/route-failover-configs/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteConfigAsync(int id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/route-failover-configs/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
