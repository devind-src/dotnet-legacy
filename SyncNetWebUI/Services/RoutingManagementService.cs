using System.Net.Http.Json;
using SyncNetApi.Dtos.Routing;

namespace SyncNetWasm.Services
{
    /// <summary>Tombol "Terapkan Perubahan": kirim RESYNC ke aplikasi yang memakai routing.</summary>
    public class RoutingManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public RoutingManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<(RoutingApplyResultDto? Result, string? Error)> ApplyAsync(CancellationToken ct = default)
        {
            var response = await Client.PostAsync("api/v1/routing/apply", null, ct);
            if (!response.IsSuccessStatusCode)
                return (null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (await response.Content.ReadFromJsonAsync<RoutingApplyResultDto>(cancellationToken: ct), null);
        }
    }
}
