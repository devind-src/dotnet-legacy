using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    [Table("dashboard_user_reset", Schema = "public")]
    public class DashboardUserReset
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long id { get; set; }

        public string email { get; set; } = string.Empty;

        /// <summary>SHA-256 hex hash of the raw reset token — never the raw token itself
        /// (same pattern as RefreshToken.TokenHash).</summary>
        public string vcode { get; set; } = string.Empty;
        public DateTime time_request { get; set; }

        /// <summary>0 = requested, 1 = no longer usable (either consumed — see consumed_at —
        /// or superseded by a newer request for the same user).</summary>
        public string status { get; set; } = "0";

        /// <summary>Resolved once at request time so validation never has to re-resolve a
        /// (possibly ambiguous, pre-unique-index) email back to an account. Nullable because
        /// pre-existing rows from before this column existed won't have it.</summary>
        public string? user_name { get; set; }

        public DateTime? consumed_at { get; set; }
        public string? requested_ip { get; set; }
    }
}
