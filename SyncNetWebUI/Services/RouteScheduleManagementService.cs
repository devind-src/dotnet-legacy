using System.Globalization;
using System.Net.Http.Json;
using SyncNetApi.Dtos.RouteSchedules;

namespace SyncNetWasm.Services
{
    /// <summary>Routing &gt; Jadwal Routing (api/v1/route-schedules), Fase 3.</summary>
    public class RouteScheduleManagementService
    {
        private const string Base = "api/v1/route-schedules";
        private readonly IHttpClientFactory _httpClientFactory;

        public RouteScheduleManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<RouteScheduleDto>?> GetSchedulesAsync(string? filter, IEnumerable<string> states, int? nodeId, string? ruleType,
            CancellationToken ct = default)
        {
            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(filter)) query.Add($"filter={Uri.EscapeDataString(filter)}");
            var s = string.Join(",", states);
            if (s.Length > 0) query.Add($"states={Uri.EscapeDataString(s)}");
            if (nodeId.HasValue) query.Add($"nodeId={nodeId}");
            if (!string.IsNullOrWhiteSpace(ruleType)) query.Add($"ruleType={Uri.EscapeDataString(ruleType)}");

            var response = await Client.GetAsync(Base + (query.Count > 0 ? "?" + string.Join("&", query) : ""), ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<RouteScheduleDto>>(cancellationToken: ct) : null;
        }

        public async Task<List<RouteScheduleHistDto>?> GetHistoryAsync(int id, CancellationToken ct = default)
        {
            var response = await Client.GetAsync($"{Base}/{id}/history", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<RouteScheduleHistDto>>(cancellationToken: ct) : null;
        }

        public async Task<(ScheduleCheckResultDto? Result, string? Error)> CheckAsync(DateTime at, string? productId, int? nodeId, int? denom,
            CancellationToken ct = default)
        {
            var query = new List<string> { $"at={Uri.EscapeDataString(at.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture))}" };
            if (!string.IsNullOrWhiteSpace(productId)) query.Add($"productId={Uri.EscapeDataString(productId)}");
            if (nodeId.HasValue) query.Add($"nodeId={nodeId}");
            if (denom.HasValue) query.Add($"denom={denom}");

            var response = await Client.GetAsync($"{Base}/check?{string.Join("&", query)}", ct);
            return response.IsSuccessStatusCode
                ? (await response.Content.ReadFromJsonAsync<ScheduleCheckResultDto>(cancellationToken: ct), null)
                : (null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(ScheduleCheckResultDto? Result, string? Error)> PreviewAsync(SaveRouteScheduleRequest request, int? editingId,
            CancellationToken ct = default)
        {
            var url = $"{Base}/preview" + (editingId.HasValue ? $"?editingId={editingId}" : "");
            var response = await Client.PostAsJsonAsync(url, request, ct);
            return response.IsSuccessStatusCode
                ? (await response.Content.ReadFromJsonAsync<ScheduleCheckResultDto>(cancellationToken: ct), null)
                : (null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> CreateAsync(SaveRouteScheduleRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync(Base, request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(int id, SaveRouteScheduleRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"{Base}/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> CancelAsync(int id, string reason, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync($"{Base}/{id}/cancel", new CancelRouteScheduleRequest { Reason = reason }, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"{Base}/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
