using System.Net.Http.Json;
using SyncNetApi.Dtos.VaTransRequests;

namespace SyncNetWasm.Services
{
    /// <summary>Backs Virtual Account &gt; Topup, Adjustment and Approval — three pages over
    /// the same api/v1/va-{topup,adjustment,approval} family, matching VaTransRequestService
    /// on the API side.</summary>
    public class VaTransRequestManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public VaTransRequestManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public Task<List<VaTransRequestDto>?> GetTopupRecordsAsync(string? filter = null, CancellationToken ct = default)
            => GetRecordsAsync("api/v1/va-topup", filter, ct);

        public Task<List<VaTransRequestDto>?> GetAdjustmentRecordsAsync(string? filter = null, CancellationToken ct = default)
            => GetRecordsAsync("api/v1/va-adjustment", filter, ct);

        public Task<List<VaTransRequestDto>?> GetApprovalRecordsAsync(string? filter = null, CancellationToken ct = default)
            => GetRecordsAsync("api/v1/va-approval", filter, ct);

        private async Task<List<VaTransRequestDto>?> GetRecordsAsync(string basePath, string? filter, CancellationToken ct)
        {
            var url = basePath + (string.IsNullOrWhiteSpace(filter) ? "" : $"?filter={Uri.EscapeDataString(filter)}");
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<VaTransRequestDto>>(cancellationToken: ct) : null;
        }

        public async Task<VaTransRequestDto?> GetByIdAsync(string basePath, int tranNr, CancellationToken ct = default)
        {
            var response = await Client.GetAsync($"{basePath}/{tranNr}", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<VaTransRequestDto>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, VaTransRequestDto? Request, string? Error)> CreateTopupAsync(CreateVaTopupRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/va-topup", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<VaTransRequestDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, VaTransRequestDto? Request, string? Error)> CreateAdjustmentAsync(CreateVaAdjustmentRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/va-adjustment", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<VaTransRequestDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> ApproveAsync(int tranNr, CancellationToken ct = default)
        {
            var response = await Client.PostAsync($"api/v1/va-approval/{tranNr}/approve", null, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> RejectAsync(int tranNr, CancellationToken ct = default)
        {
            var response = await Client.PostAsync($"api/v1/va-approval/{tranNr}/reject", null, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
