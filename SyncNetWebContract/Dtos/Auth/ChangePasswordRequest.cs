using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Auth
{
    public class ChangePasswordRequest
    {
        [Required]
        public string OldPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(8, ErrorMessage = "New password must be at least 8 characters long.")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
