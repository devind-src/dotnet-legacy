using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace SyncNetWasm.Services
{
    /// <summary>
    /// Purely in-memory reflection of "what AuthService currently believes" — it never
    /// itself decides whether a session is valid. AuthService.InitializeAsync (silent
    /// refresh on startup), LoginAsync, and LogoutLocalAsync are the only callers that
    /// mutate this.
    /// </summary>
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));
        private AuthenticationState _current = Anonymous;

        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(_current);

        public void NotifyAuthenticated(string userName, string roleName)
        {
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, userName), new Claim(ClaimTypes.Role, roleName)],
                authenticationType: "jwt");

            _current = new AuthenticationState(new ClaimsPrincipal(identity));
            NotifyAuthenticationStateChanged(Task.FromResult(_current));
        }

        public void NotifyLoggedOut()
        {
            _current = Anonymous;
            NotifyAuthenticationStateChanged(Task.FromResult(_current));
        }
    }
}
