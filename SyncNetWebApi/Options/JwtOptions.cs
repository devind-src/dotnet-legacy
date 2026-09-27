namespace SyncNetApi.Options
{
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;

        /// <summary>
        /// Symmetric signing key (min 32 chars / 256 bit). In production, source this from
        /// an environment variable or a secret manager (Azure Key Vault, AWS Secrets Manager,
        /// dotnet user-secrets for local dev) — never commit the real value.
        /// </summary>
        public string SigningKey { get; set; } = string.Empty;

        public int AccessTokenMinutes { get; set; } = 15;
        public int RefreshTokenDays { get; set; } = 7;
    }
}
