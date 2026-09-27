using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>
    /// Stores the JTI (JWT ID) of access tokens that were explicitly logged out before
    /// their natural expiry. Checked on every authenticated request via
    /// JwtBearerEvents.OnTokenValidated. Rows are safe to purge once ExpiresAt has passed
    /// (see BlacklistCleanupService).
    /// </summary>
    [Table("api_token_blacklist", Schema = "public")]
    public class BlacklistedToken
    {
        [Key]
        public string Jti { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        /// <summary>Original access token expiry - after this, the row is dead weight and can be purged.</summary>
        public DateTime ExpiresAt { get; set; }

        public DateTime RevokedAt { get; set; } = DateTime.UtcNow;
    }
}
