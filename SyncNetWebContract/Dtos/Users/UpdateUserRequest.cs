using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Users
{
    public class UpdateUserRequest
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string RoleName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Status { get; set; } = string.Empty;

        /// <summary>Optional per-user override of the role's add/edit/delete rights.
        /// Null clears back to "inherit the role" — this is a full replace, like the rest
        /// of PUT.</summary>
        public bool? AllowAdd { get; set; }
        public bool? AllowEdit { get; set; }
        public bool? AllowDelete { get; set; }
    }

    public class ResetPasswordRequest
    {
        [Required, MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
