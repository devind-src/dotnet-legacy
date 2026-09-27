using System.Net.Http.Json;
using SyncNetApi.Dtos.PosnetBins;

namespace SyncNetWasm.Services
{
    public class PosnetBinManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public PosnetBinManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<PosnetBinDto>?> GetBinsAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = string.IsNullOrWhiteSpace(filter) ? "api/v1/posnet-bins" : $"api/v1/posnet-bins?filter={Uri.EscapeDataString(filter)}";
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<PosnetBinDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, PosnetBinDto? Bin, string? Error)> CreateBinAsync(CreatePosnetBinRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/posnet-bins", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<PosnetBinDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateBinAsync(int id, UpdatePosnetBinRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/posnet-bins/{id}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteBinAsync(int id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/posnet-bins/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
