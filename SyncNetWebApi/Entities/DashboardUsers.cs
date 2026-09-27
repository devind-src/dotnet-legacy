using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    [Table("dashboard_user", Schema = "public")]
    public class DashboardUsers
    {
        [Key]
        public string user_name { get; set; } = string.Empty;

        // NOTE: these are all `nullable=YES` on the real table even though a couple (state)
        // actually contain NULL for existing rows — mapping them non-nullable made EF throw
        // InvalidCastException the moment such a row was materialized, which in practice
        // meant AuthController.Login crashed outright for those accounts. Kept nullable to
        // match the schema truthfully; callers coalesce to a sane default where a non-null
        // value is actually required (see AuthController/UserService).
        public string? full_name { get; set; }
        public string? role_name { get; set; }
        public string? email { get; set; }

        /// <summary>
        /// Password hash. New rows use the modern ASP.NET Core Identity PBKDF2 format
        /// (see PasswordService). Rows created by the legacy Blazor app still contain the
        /// old reversible DES value until the user logs in again and gets auto-upgraded.
        /// </summary>
        public string? password { get; set; }

        public int? max_retry { get; set; } = 3;
        public int retry { get; set; }
        public string? state { get; set; }
        public string? status { get; set; } = "1";
        public DateTime? last_login { get; set; }

        /// <summary>
        /// Forces a password change before the account can do anything else. Set true by
        /// admin-reset; cleared once the user successfully sets their own new password
        /// (self-service reset, or the forced-change login challenge). Nullable in the DB
        /// (existing rows may not have been backfilled) — treat null as false.
        /// </summary>
        public bool? must_change_password { get; set; } = false;

        /// <summary>
        /// Per-user override of dashboard_role.flag_add/flag_edit/flag_delete. Null means
        /// "inherit whatever the role currently grants" (re-evaluated live, not a one-time
        /// copy); true/false explicitly overrides the role for this one user. See
        /// IUserService.GetEffectivePermissionsAsync for the resolution rule.
        /// </summary>
        public bool? allow_add { get; set; }
        public bool? allow_edit { get; set; }
        public bool? allow_delete { get; set; }
    }
}
