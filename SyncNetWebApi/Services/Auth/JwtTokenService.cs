using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SyncNetApi.Options;

namespace SyncNetApi.Services.Auth
{
    /// <summary>
    /// Uses Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler — the modern,
    /// higher-performance replacement for the older System.IdentityModel.Tokens.Jwt
    /// handler, recommended for new development.
    /// </summary>
    public class JwtTokenService : IJwtTokenService
    {
        private readonly JwtOptions _options;
        private readonly SigningCredentials _signingCredentials;
        private readonly JsonWebTokenHandler _handler = new();

        public JwtTokenService(IOptions<JwtOptions> options)
        {
            _options = options.Value;

            if (string.IsNullOrWhiteSpace(_options.SigningKey) || Encoding.UTF8.GetByteCount(_options.SigningKey) < 32)
                throw new InvalidOperationException(
                    "Jwt:SigningKey must be configured and at least 32 bytes (256 bits) long. " +
                    "Set it via user-secrets or an environment variable, never commit it to source control.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
            _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        }

        public AccessTokenResult CreateAccessToken(string userName, string roleName, IDictionary<string, string>? extraClaims = null)
        {
            var jti = Guid.NewGuid().ToString("N");
            var now = DateTime.UtcNow;
            var expires = now.AddMinutes(_options.AccessTokenMinutes);

            var claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = userName,
                [JwtRegisteredClaimNames.Jti] = jti,
                [ClaimTypes.Role] = roleName,
            };

            if (extraClaims != null)
                foreach (var (k, v) in extraClaims)
                    claims[k] = v;

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _options.Issuer,
                Audience = _options.Audience,
                Claims = claims,
                NotBefore = now,
                Expires = expires,
                SigningCredentials = _signingCredentials
            };

            var token = _handler.CreateToken(descriptor);
            return new AccessTokenResult(token, jti, expires);
        }

        public string CreateRefreshToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes);
        }

        public string HashRefreshToken(string rawToken)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToHexString(bytes);
        }
    }
}
