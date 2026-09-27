using System.Net.Http.Json;
using SyncNetApi.Dtos.TerminalImages;

namespace SyncNetWasm.Services
{
    public class TerminalImageManagementService
    {
        private const long MaxUploadBytes = 5 * 1024 * 1024;
        private readonly IHttpClientFactory _httpClientFactory;

        public TerminalImageManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<TerminalImageDto>?> GetImagesAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = string.IsNullOrWhiteSpace(filter) ? "api/v1/terminal-images" : $"api/v1/terminal-images?filter={Uri.EscapeDataString(filter)}";
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<TerminalImageDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, TerminalImageDto? Image, string? Error)> CreateImageAsync(CreateTerminalImageRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/terminal-images", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<TerminalImageDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateImageAsync(string name, UpdateTerminalImageRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/terminal-images/{Uri.EscapeDataString(name)}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteImageAsync(string name, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/terminal-images/{Uri.EscapeDataString(name)}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, TerminalImageDto? Image, string? Error)> UploadBannerAsync(
            string name, int slot, Microsoft.AspNetCore.Components.Forms.IBrowserFile file, CancellationToken ct = default)
        {
            using var content = new MultipartFormDataContent();
            using var stream = file.OpenReadStream(MaxUploadBytes, ct);
            using var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            content.Add(streamContent, "file", file.Name);

            var response = await Client.PostAsync($"api/v1/terminal-images/{Uri.EscapeDataString(name)}/banners/{slot}", content, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<TerminalImageDto>(cancellationToken: ct), null);
        }
    }
}
