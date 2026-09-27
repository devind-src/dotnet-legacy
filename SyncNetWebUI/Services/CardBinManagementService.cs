using System.Net.Http.Json;
using SyncNetApi.Dtos.CardBins;

namespace SyncNetWasm.Services
{
    public class CardBinManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CardBinManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<CardBinDto>?> GetBinsAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/card-bins" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<CardBinDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, CardBinDto? Bin, string? Error)> CreateBinAsync(CreateCardBinRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/card-bins", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<CardBinDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateBinAsync(string binNr, UpdateCardBinRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/card-bins/{Uri.EscapeDataString(binNr)}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteBinAsync(string binNr, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/card-bins/{Uri.EscapeDataString(binNr)}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
