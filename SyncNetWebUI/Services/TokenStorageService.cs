using Blazored.LocalStorage;

namespace SyncNetWasm.Services
{
    /// <summary>
    /// Access token lives in memory only (never written to disk) — cleared on tab close or
    /// reload. Refresh token is persisted to localStorage so a page reload doesn't force a
    /// full re-login; AuthService.InitializeAsync uses it to silently mint a fresh access
    /// token on startup. This mirrors the split already built into SyncNetApi: refresh
    /// tokens are the long-lived, storable credential; access tokens are short-lived and
    /// meant to be re-minted often.
    /// </summary>
    public class TokenStorageService
    {
        private const string RefreshTokenKey = "syncnet.refreshToken";

        private readonly ILocalStorageService _localStorage;

        public string? AccessToken { get; private set; }
        public DateTime? AccessTokenExpiresAt { get; private set; }

        public TokenStorageService(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public void SetAccessToken(string token, DateTime expiresAt)
        {
            AccessToken = token;
            AccessTokenExpiresAt = expiresAt;
        }

        public void ClearAccessToken()
        {
            AccessToken = null;
            AccessTokenExpiresAt = null;
        }

        public async Task SetRefreshTokenAsync(string token)
        {
            await _localStorage.SetItemAsStringAsync(RefreshTokenKey, token);
        }

        public async Task<string?> GetRefreshTokenAsync()
        {
            var token = await _localStorage.GetItemAsStringAsync(RefreshTokenKey);
            return string.IsNullOrEmpty(token) ? null : token;
        }

        public async Task ClearRefreshTokenAsync()
        {
            await _localStorage.RemoveItemAsync(RefreshTokenKey);
        }
    }
}
