using System.Net.Http.Json;
using SyncNetApi.Dtos.CardIssuers;

namespace SyncNetWasm.Services
{
    public class CardIssuerManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CardIssuerManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<CardIssuerDto>?> GetIssuersAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/card-issuers" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<CardIssuerDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, CardIssuerDto? Issuer, string? Error)> CreateIssuerAsync(CreateCardIssuerRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/card-issuers", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<CardIssuerDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateIssuerAsync(string issuer, UpdateCardIssuerRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/card-issuers/{Uri.EscapeDataString(issuer)}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteIssuerAsync(string issuer, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/card-issuers/{Uri.EscapeDataString(issuer)}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
