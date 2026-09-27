using System.Net.Http.Json;
using SyncNetApi.Dtos.PublicHolidays;

namespace SyncNetWasm.Services
{
    public class PublicHolidayManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public PublicHolidayManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<PublicHolidayDto>?> GetHolidaysAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/public-holidays" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<PublicHolidayDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, PublicHolidayDto? Holiday, string? Error)> CreateHolidayAsync(CreatePublicHolidayRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/public-holidays", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<PublicHolidayDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateHolidayAsync(string holidayDate, UpdatePublicHolidayRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/public-holidays/{Uri.EscapeDataString(holidayDate)}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteHolidayAsync(string holidayDate, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/public-holidays/{Uri.EscapeDataString(holidayDate)}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
