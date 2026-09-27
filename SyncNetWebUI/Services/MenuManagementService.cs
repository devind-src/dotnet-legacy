using System.Net.Http.Json;
using SyncNetApi.Dtos.Menus;

namespace SyncNetWasm.Services
{
    /// <summary>CRUD against api/v1/menus. Mirrors UserManagementService's pattern: always the
    /// "Api" named client (bearer token + auto-retry-once-on-401), same (Success, Error) tuple
    /// shape for mutations.</summary>
    public class MenuManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public MenuManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<MenuDto>?> GetMenusAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = string.IsNullOrWhiteSpace(filter) ? "api/v1/menus" : $"api/v1/menus?filter={Uri.EscapeDataString(filter)}";
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<MenuDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, MenuDto? Menu, string? Error)> CreateMenuAsync(CreateMenuRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/menus", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<MenuDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateMenuAsync(int menuId, UpdateMenuRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/menus/{menuId}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteMenuAsync(int menuId, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/menus/{menuId}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
