using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Http;
using SyncNetApi.Dtos.Auth;

namespace SyncNetWasm.Services
{
    /// <summary>
    /// Owns the whole auth lifecycle: login (incl. the NEW_PASSWORD_REQUIRED challenge),
    /// silent refresh, logout, and self-service password flows. Uses the unauthenticated
    /// "ApiAnonymous" HttpClient for anything that doesn't need (or doesn't yet have) a
    /// bearer token — this is what breaks the circular dependency with
    /// AuthorizedHttpMessageHandler, which resolves this class only lazily (via
    /// IServiceProvider, on a 401) rather than through constructor injection.
    /// </summary>
    public class AuthService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TokenStorageService _tokens;
        private readonly CustomAuthStateProvider _authState;
        private readonly SessionState _session;

        private bool _initialized;

        // Coalesces concurrent refresh attempts into a single in-flight call. Several
        // requests can 401 at once (e.g. multiple widgets loading in parallel right as the
        // 15-min access token expires) and each independently calls TryRefreshAsync via
        // AuthorizedHttpMessageHandler; the refresh token is single-use with rotation, so
        // firing it twice concurrently would burn it on the first call and trip the server's
        // reuse-detection (unwanted whole-family revocation, logging the user out) on the
        // second. The lock only guards the synchronous check-and-set of _refreshInFlight, not
        // the refresh itself.
        private readonly object _refreshLock = new();
        private Task<bool>? _refreshInFlight;

        // Refreshes a few minutes ahead of actual expiry (access tokens are 15-min-lived) so
        // ordinary requests almost never hit the reactive 401-then-refresh-then-retry path in
        // AuthorizedHttpMessageHandler — that path still exists as a fallback (clock skew,
        // tab asleep past the scheduled time, etc.), this just makes it rare in practice.
        private static readonly TimeSpan ProactiveRefreshMargin = TimeSpan.FromMinutes(2);
        private CancellationTokenSource? _proactiveRefreshCts;

        public AuthService(IHttpClientFactory httpClientFactory, TokenStorageService tokens, CustomAuthStateProvider authState, SessionState session)
        {
            _httpClientFactory = httpClientFactory;
            _tokens = tokens;
            _authState = authState;
            _session = session;
        }

        private const string UnreachableMessage = "Tidak dapat terhubung ke server. Periksa koneksi Anda dan coba lagi.";

        private HttpClient AnonymousClient => _httpClientFactory.CreateClient("ApiAnonymous");
        private HttpClient AuthorizedClient => _httpClientFactory.CreateClient("Api");

        /// <summary>The named HttpClients carry a bounded Timeout (see Program.cs) so a down
        /// or unreachable API fails fast rather than hanging indefinitely — HttpClient reports
        /// that as a thrown TaskCanceledException/OperationCanceledException (not a normal
        /// response), same as an outright connection failure (HttpRequestException). Both are
        /// caught here so callers (e.g. Login.razor) always get a normal failure result back
        /// instead of an unhandled exception that would leave a busy-spinner stuck forever.</summary>
        private static bool IsConnectivityException(Exception ex) =>
            ex is HttpRequestException or TaskCanceledException or OperationCanceledException;

        /// <summary>Call once at app startup (see App.razor) — attempts a silent refresh
        /// using whatever refresh token is in localStorage, so a page reload doesn't force
        /// the user back to the login page while their session is still valid.</summary>
        public async Task InitializeAsync(CancellationToken ct = default)
        {
            if (_initialized) return;
            _initialized = true;
            await TryRefreshAsync(ct);
        }

        /// <summary>Fetches a fresh login captcha. Null when the API is unreachable; the caller
        /// shows a retry hint. Enabled = false means the server does not require one.</summary>
        public async Task<CaptchaResponse?> GetCaptchaAsync(CancellationToken ct = default)
        {
            try
            {
                var response = await AnonymousClient.GetAsync("api/v1/auth/captcha", ct);
                return response.IsSuccessStatusCode
                    ? await response.Content.ReadFromJsonAsync<CaptchaResponse>(JsonOptions, ct)
                    : null;
            }
            catch (Exception ex) when (IsConnectivityException(ex))
            {
                return null;
            }
        }

        public async Task<LoginResult> LoginAsync(string userName, string password,
            string? captchaId = null, string? captchaAnswer = null, CancellationToken ct = default)
        {
            try
            {
                var response = await AnonymousClient.PostAsJsonAsync("api/v1/auth/login",
                    new LoginRequest { UserName = userName, Password = password, CaptchaId = captchaId, CaptchaAnswer = captchaAnswer }, ct);
                return await HandleLoginResponseAsync(response, ct);
            }
            catch (Exception ex) when (IsConnectivityException(ex))
            {
                return LoginResult.Failed(UnreachableMessage);
            }
        }

        public async Task<LoginResult> CompleteNewPasswordAsync(string userName, string currentPassword, string newPassword, CancellationToken ct = default)
        {
            try
            {
                var response = await AnonymousClient.PostAsJsonAsync("api/v1/auth/login/new-password",
                    new CompleteNewPasswordRequest { UserName = userName, CurrentPassword = currentPassword, NewPassword = newPassword }, ct);
                return await HandleLoginResponseAsync(response, ct);
            }
            catch (Exception ex) when (IsConnectivityException(ex))
            {
                return LoginResult.Failed(UnreachableMessage);
            }
        }

        private async Task<LoginResult> HandleLoginResponseAsync(HttpResponseMessage response, CancellationToken ct)
        {
            if (!response.IsSuccessStatusCode)
            {
                var detail = await HttpErrorHelper.TryReadProblemDetailAsync(response, ct);
                return LoginResult.Failed(detail ?? $"Request failed ({(int)response.StatusCode}).");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            if (doc.RootElement.TryGetProperty("challengeRequired", out _))
            {
                var challenge = doc.RootElement.Deserialize<LoginChallengeResponse>(JsonOptions)!;
                return LoginResult.Challenge(challenge.UserName);
            }

            var login = doc.RootElement.Deserialize<LoginResponse>(JsonOptions)!;
            await PersistSessionAsync(login);
            return LoginResult.Success();
        }

        private async Task PersistSessionAsync(LoginResponse login)
        {
            _tokens.SetAccessToken(login.AccessToken, login.AccessTokenExpiresAt);
            await _tokens.SetRefreshTokenAsync(login.RefreshToken);
            _session.Set(login);
            _authState.NotifyAuthenticated(login.UserName, login.RoleName);
            ScheduleProactiveRefresh(login.AccessTokenExpiresAt);
        }

        /// <summary>(Re)schedules a one-shot proactive refresh ahead of the current access
        /// token's expiry. Cancels any previously-scheduled one first — called again on every
        /// login/refresh, so only the latest token's expiry is ever being counted down to.</summary>
        private void ScheduleProactiveRefresh(DateTime accessTokenExpiresAt)
        {
            CancelProactiveRefresh();

            _proactiveRefreshCts = new CancellationTokenSource();
            _ = RunProactiveRefreshAsync(accessTokenExpiresAt, _proactiveRefreshCts.Token);
        }

        private async Task RunProactiveRefreshAsync(DateTime accessTokenExpiresAt, CancellationToken ct)
        {
            var delay = accessTokenExpiresAt - ProactiveRefreshMargin - DateTime.UtcNow;
            if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;

            try
            {
                await Task.Delay(delay, ct);
                await TryRefreshAsync();
            }
            catch (OperationCanceledException)
            {
                // Superseded by a newer login/refresh, or the user logged out — nothing to do.
            }
        }

        private void CancelProactiveRefresh()
        {
            _proactiveRefreshCts?.Cancel();
            _proactiveRefreshCts?.Dispose();
            _proactiveRefreshCts = null;
        }

        /// <summary>Used both at startup (InitializeAsync) and by AuthorizedHttpMessageHandler
        /// after a 401. Never throws — returns false on any failure (missing/expired/invalid
        /// refresh token, network error). Concurrent callers are coalesced onto the same
        /// underlying refresh call (see _refreshInFlight) instead of each firing their own.</summary>
        public Task<bool> TryRefreshAsync(CancellationToken ct = default)
        {
            lock (_refreshLock)
            {
                return _refreshInFlight ??= RunRefreshAsync();
            }
        }

        /// <summary>The actual refresh call, run at most once concurrently. Deliberately not
        /// tied to any single caller's CancellationToken — it's shared by every request that
        /// was waiting on the same 401, and one caller's request being aborted (e.g. its
        /// component was disposed after a navigation) must not cancel the refresh for the
        /// others still waiting on it.</summary>
        private async Task<bool> RunRefreshAsync()
        {
            try
            {
                var rawRefresh = await _tokens.GetRefreshTokenAsync();
                if (string.IsNullOrEmpty(rawRefresh)) return false;

                try
                {
                    var response = await AnonymousClient.PostAsJsonAsync("api/v1/auth/refresh",
                        new RefreshTokenRequest { RefreshToken = rawRefresh });

                    if (!response.IsSuccessStatusCode)
                    {
                        await LogoutLocalAsync();
                        return false;
                    }

                    var login = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
                    if (login == null) return false;

                    await PersistSessionAsync(login);
                    return true;
                }
                catch (Exception ex) when (IsConnectivityException(ex))
                {
                    // Network/API unreachable — leave local state as-is rather than logging out;
                    // the next successful request will retry.
                    return false;
                }
            }
            finally
            {
                lock (_refreshLock)
                {
                    _refreshInFlight = null;
                }
            }
        }

        public async Task LogoutAsync(CancellationToken ct = default)
        {
            var rawRefresh = await _tokens.GetRefreshTokenAsync();
            try
            {
                await AuthorizedClient.PostAsJsonAsync("api/v1/auth/logout",
                    new RefreshTokenRequest { RefreshToken = rawRefresh ?? string.Empty }, ct);
            }
            catch (Exception ex) when (IsConnectivityException(ex))
            {
                // Best-effort — still clear local state even if the API call fails, so the
                // user isn't stuck "logged in" on this device just because the API is down.
            }

            await LogoutLocalAsync();
        }

        /// <summary>Clears local session state only — does not call the API. Used by
        /// AuthorizedHttpMessageHandler when a refresh attempt fails, and by LogoutAsync.</summary>
        public async Task LogoutLocalAsync()
        {
            CancelProactiveRefresh();
            _tokens.ClearAccessToken();
            await _tokens.ClearRefreshTokenAsync();
            _session.Clear();
            _authState.NotifyLoggedOut();
        }

        public async Task<(bool Success, string? Error)> ForgotPasswordAsync(string identifier, CancellationToken ct = default)
        {
            try
            {
                var response = await AnonymousClient.PostAsJsonAsync("api/v1/auth/forgot-password",
                    new ForgotPasswordRequest { Identifier = identifier }, ct);
                return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
            }
            catch (Exception ex) when (IsConnectivityException(ex))
            {
                return (false, UnreachableMessage);
            }
        }

        public async Task<(bool Success, string? Error)> ResetPasswordAsync(string token, string newPassword, CancellationToken ct = default)
        {
            try
            {
                var response = await AnonymousClient.PostAsJsonAsync("api/v1/auth/reset-password",
                    new CompletePasswordResetRequest { Token = token, NewPassword = newPassword }, ct);
                return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
            }
            catch (Exception ex) when (IsConnectivityException(ex))
            {
                return (false, UnreachableMessage);
            }
        }

        public async Task<(bool Success, string? Error)> ChangePasswordAsync(string oldPassword, string newPassword, CancellationToken ct = default)
        {
            try
            {
                var response = await AuthorizedClient.PostAsJsonAsync("api/v1/auth/change-password",
                    new ChangePasswordRequest { OldPassword = oldPassword, NewPassword = newPassword }, ct);
                return response.IsSuccessStatusCode ? (true, null) : (false, await HttpErrorHelper.TryReadProblemDetailAsync(response, ct));
            }
            catch (Exception ex) when (IsConnectivityException(ex))
            {
                return (false, UnreachableMessage);
            }
        }
    }
}
