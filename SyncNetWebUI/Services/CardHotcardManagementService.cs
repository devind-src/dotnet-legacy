using System.Net.Http.Json;
using SyncNetApi.Dtos.CardHotcards;

namespace SyncNetWasm.Services
{
    public class CardHotcardManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CardHotcardManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<CardHotcardDto>?> GetHotcardsAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/card-hotcards" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<CardHotcardDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, CardHotcardDto? Hotcard, string? Error)> CreateHotcardAsync(CreateCardHotcardRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/card-hotcards", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<CardHotcardDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateHotcardAsync(string cardNr, UpdateCardHotcardRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/card-hotcards/{Uri.EscapeDataString(cardNr)}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteHotcardAsync(string cardNr, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/card-hotcards/{Uri.EscapeDataString(cardNr)}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
