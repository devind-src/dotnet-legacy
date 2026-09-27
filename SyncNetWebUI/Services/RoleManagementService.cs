using System.Net.Http.Json;
using SyncNetApi.Dtos.Roles;

namespace SyncNetWasm.Services
{
    /// <summary>CRUD against api/v1/roles. Mirrors UserManagementService's pattern: always the
    /// "Api" named client (bearer token + auto-retry-once-on-401), same (Success, Error) tuple
    /// shape for mutations.</summary>
    public class RoleManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public RoleManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<RoleDto>?> GetRolesAsync(string? filter = null, CancellationToken ct = default)
        {
            var url = string.IsNullOrWhiteSpace(filter) ? "api/v1/roles" : $"api/v1/roles?filter={Uri.EscapeDataString(filter)}";
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<RoleDto>>(cancellationToken: ct) : null;
        }

        public async Task<RoleDetailDto?> GetRoleAsync(string roleName, CancellationToken ct = default)
        {
            var response = await Client.GetAsync($"api/v1/roles/{Uri.EscapeDataString(roleName)}", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<RoleDetailDto>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, RoleDetailDto? Role, string? Error)> CreateRoleAsync(CreateRoleRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/roles", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<RoleDetailDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateRoleAsync(string roleName, UpdateRoleRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/roles/{Uri.EscapeDataString(roleName)}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteRoleAsync(string roleName, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/roles/{Uri.EscapeDataString(roleName)}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
