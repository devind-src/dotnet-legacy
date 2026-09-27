using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Users
{
    public class CreateUserRequest
    {
        [Required, MaxLength(50)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string RoleName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(8)]
        public string Password { get; set; } = string.Empty;

        /// <summary>Optional per-user override of the role's add/edit/delete rights.
        /// Omit (null) to just inherit whatever the assigned role currently grants.</summary>
        public bool? AllowAdd { get; set; }
        public bool? AllowEdit { get; set; }
        public bool? AllowDelete { get; set; }
    }
}
