using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>
    /// Refresh tokens are stored hashed (never the raw value) and rotated on every use:
    /// each refresh issues a brand new refresh token and revokes the old one. If a
    /// revoked token is presented again, the whole family is revoked (replay/theft signal).
    /// </summary>
    [Table("api_refresh_token", Schema = "public")]
    public class RefreshToken
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string TokenHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }

        public string? ReplacedByTokenHash { get; set; }
        public string? CreatedByIp { get; set; }

        [NotMapped]
        public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
    }
}
