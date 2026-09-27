using System.Net.Http.Json;
using SyncNetApi.Dtos.Roles;
using SyncNetApi.Dtos.Users;

namespace SyncNetWasm.Services
{
    /// <summary>CRUD against api/v1/users plus the api/v1/roles lookup used to populate the
    /// Role dropdown. Mirrors AuthService's pattern: always the "Api" named client (bearer
    /// token + auto-retry-once-on-401), same (Success, Error) tuple shape for mutations.</summary>
    public class UserManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public UserManagementService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<List<UserDto>?> GetUsersAsync(string? filter, CancellationToken ct = default)
        {
            var url = string.IsNullOrWhiteSpace(filter) ? "api/v1/users" : $"api/v1/users?filter={Uri.EscapeDataString(filter)}";
            var response = await Client.GetAsync(url, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<UserDto>>(cancellationToken: ct) : null;
        }

        public async Task<UserDto?> GetUserAsync(string userName, CancellationToken ct = default)
        {
            var response = await Client.GetAsync($"api/v1/users/{Uri.EscapeDataString(userName)}", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken: ct) : null;
        }

        public async Task<List<RoleDto>?> GetRolesAsync(CancellationToken ct = default)
        {
            var response = await Client.GetAsync("api/v1/roles", ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<RoleDto>>(cancellationToken: ct) : null;
        }

        public async Task<(bool Success, UserDto? User, string? Error)> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync("api/v1/users", request, ct);
            if (!response.IsSuccessStatusCode)
                return (false, null, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));

            return (true, await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken: ct), null);
        }

        public async Task<(bool Success, string? Error)> UpdateUserAsync(string userName, UpdateUserRequest request, CancellationToken ct = default)
        {
            var response = await Client.PutAsJsonAsync($"api/v1/users/{Uri.EscapeDataString(userName)}", request, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> DeleteUserAsync(string userName, CancellationToken ct = default)
        {
            var response = await Client.DeleteAsync($"api/v1/users/{Uri.EscapeDataString(userName)}", ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }

        public async Task<(bool Success, string? Error)> ResetPasswordAsync(string userName, string newPassword, CancellationToken ct = default)
        {
            var response = await Client.PostAsJsonAsync($"api/v1/users/{Uri.EscapeDataString(userName)}/reset-password",
                new ResetPasswordRequest { NewPassword = newPassword }, ct);
            return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
        }
    }
}
