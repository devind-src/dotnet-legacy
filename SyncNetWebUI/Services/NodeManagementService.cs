using System.Net.Http.Json;
using SyncNetApi.Dtos.Nodes;

namespace SyncNetWasm.Services
{
    public class NodeManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public NodeManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<NodeDto>?> GetNodesAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/nodes" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<NodeDto>>(cancellationToken: ct) : null;
        }

        public async Task<(string? PortIn, string? PortOut)> GetNewPortsAsync(CancellationToken ct = default)
        {
            var response = await Client.GetAsync("api/v1/nodes/new-ports", ct);
            if (!response.IsSuccessStatusCode) return (null, null);
            var result = await response.Content.ReadFromJsonAsync<NewPortsResponse>(cancellationToken: ct);
            return (result?.PortIn, result?.PortOut);
        }

        public async Task<(bool Success, NodeDto? Node, string? Error)> CreateNodeAsync(CreateNodeRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/nodes", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<NodeDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateNodeAsync(int nodeId, UpdateNodeRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/nodes/{nodeId}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteNodeAsync(int nodeId, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/nodes/{nodeId}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        private record NewPortsResponse(string? PortIn, string? PortOut);
    }
}
