using System.Collections.Generic;

namespace SyncNetApi.Services.Auth
{
    public record AccessTokenResult(string Token, string Jti, DateTime ExpiresAt);

    public interface IJwtTokenService
    {
        AccessTokenResult CreateAccessToken(string userName, string roleName, IDictionary<string, string>? extraClaims = null);

        /// <summary>Generates a cryptographically random opaque refresh token (not a JWT).</summary>
        string CreateRefreshToken();

        string HashRefreshToken(string rawToken);
    }
}
