using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using SyncNetApi.Dtos.JobFees;

namespace SyncNetWasm.Services
{
    public class JobFeeManagementService
    {
        private const long MaxUploadBytes = 5 * 1024 * 1024;
        private readonly IHttpClientFactory _httpClientFactory;

        public JobFeeManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<JobFeeDto>?> GetJobFeesAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = "api/v1/job-fees" + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<JobFeeDto>>(cancellationToken: ct) : null;
        }

        public async Task<List<JobFeeDetailDto>?> GetDetailsAsync(long id, CancellationToken ct = default)
        {
            var response = await Client.GetAsync($"api/v1/job-fees/{id}/details", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<JobFeeDetailDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, JobFeePreviewResultDto? Preview, string? Error)> PreviewJobFeeAsync(IBrowserFile file, CancellationToken ct = default)
        {
            using var content = new MultipartFormDataContent();
            using var stream = file.OpenReadStream(MaxUploadBytes, ct);
            using var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(string.IsNullOrEmpty(file.ContentType) ? "text/csv" : file.ContentType);
            content.Add(streamContent, "file", file.Name);

            var response = await Client.PostAsync("api/v1/job-fees/preview", content, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<JobFeePreviewResultDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, JobFeeDto? JobFee, string? Error)> CreateJobFeeAsync(
            string jobDesc, DateTime? scheduledAt, IBrowserFile file, CancellationToken ct = default)
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(jobDesc), "jobDesc");
            if (scheduledAt.HasValue)
                content.Add(new StringContent(scheduledAt.Value.ToString("o")), "scheduledAt");

            using var stream = file.OpenReadStream(MaxUploadBytes, ct);
            using var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(string.IsNullOrEmpty(file.ContentType) ? "text/csv" : file.ContentType);
            content.Add(streamContent, "file", file.Name);

            var response = await Client.PostAsync("api/v1/job-fees", content, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<JobFeeDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> DeleteJobFeeAsync(long id, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/job-fees/{id}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
