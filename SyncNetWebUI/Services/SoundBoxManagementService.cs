using System.Net.Http.Json;
using SyncNetApi.Dtos.SoundBoxes;

namespace SyncNetWasm.Services
{
    public class SoundBoxManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public SoundBoxManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<SoundBoxDto>?> GetSoundBoxesAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = string.IsNullOrWhiteSpace(filter) ? "api/v1/soundboxes" : $"api/v1/soundboxes?filter={Uri.EscapeDataString(filter)}";
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<SoundBoxDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, SoundBoxDto? SoundBox, string? Error)> CreateSoundBoxAsync(CreateSoundBoxRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/soundboxes", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<SoundBoxDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateSoundBoxAsync(string nmid, UpdateSoundBoxRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/soundboxes/{Uri.EscapeDataString(nmid)}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteSoundBoxAsync(string nmid, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/soundboxes/{Uri.EscapeDataString(nmid)}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
