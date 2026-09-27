using System.Net.Http.Json;
using SyncNetApi.Dtos.JobSchedules;

namespace SyncNetWasm.Services
{
    public class JobScheduleManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public JobScheduleManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<JobScheduleDto>?> GetJobsAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/job-schedules" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<JobScheduleDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, JobScheduleDto? Job, string? Error)> CreateJobAsync(CreateJobScheduleRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/job-schedules", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<JobScheduleDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateJobAsync(int jobId, UpdateJobScheduleRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/job-schedules/{jobId}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteJobAsync(int jobId, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/job-schedules/{jobId}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
