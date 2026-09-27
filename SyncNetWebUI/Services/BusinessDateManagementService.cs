using System.Net.Http.Json;
using SyncNetApi.Dtos.BusinessDates;

namespace SyncNetWasm.Services
{
    public class BusinessDateManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public BusinessDateManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<BusinessDateDto>?> GetBusinessDatesAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/business-dates" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<BusinessDateDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, BusinessDateDto? BusinessDate, string? Error)> CreateBusinessDateAsync(CreateBusinessDateRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/business-dates", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<BusinessDateDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateBusinessDateAsync(string businessCalendar, UpdateBusinessDateRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/business-dates/{Uri.EscapeDataString(businessCalendar)}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteBusinessDateAsync(string businessCalendar, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/business-dates/{Uri.EscapeDataString(businessCalendar)}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
