using System.Net.Http.Json;
using SyncNetApi.Dtos.JobCleaners;

namespace SyncNetWasm.Services
{
    public class JobCleanerManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public JobCleanerManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<JobCleanerDto>?> GetCleanersAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/job-cleaners" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<JobCleanerDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, JobCleanerDto? Cleaner, string? Error)> CreateCleanerAsync(CreateJobCleanerRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/job-cleaners", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<JobCleanerDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateCleanerAsync(string entity, UpdateJobCleanerRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/job-cleaners/{Uri.EscapeDataString(entity)}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteCleanerAsync(string entity, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/job-cleaners/{Uri.EscapeDataString(entity)}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
