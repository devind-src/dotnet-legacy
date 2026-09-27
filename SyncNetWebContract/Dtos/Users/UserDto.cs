using System;

namespace SyncNetApi.Dtos.Users
{
    /// <summary>Never includes the password hash — this is what gets serialized back to clients.</summary>
    public class UserDto
    {
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Retry { get; set; }
        public int MaxRetry { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool MustChangePassword { get; set; }

        /// <summary>Raw per-user override — null means "inherit the role's flag_add/edit/delete".
        /// See UsersController for where the resolved (effective) value is actually enforced.</summary>
        public bool? AllowAdd { get; set; }
        public bool? AllowEdit { get; set; }
        public bool? AllowDelete { get; set; }
    }
}
