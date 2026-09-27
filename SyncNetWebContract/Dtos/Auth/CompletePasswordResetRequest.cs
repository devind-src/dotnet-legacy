using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Auth
{
    /// <summary>Completes a forgot-password flow. Named distinctly from
    /// Dtos.Users.ResetPasswordRequest (the admin-initiated one, which needs no token) to
    /// avoid confusing the two shapes.</summary>
    public class CompletePasswordResetRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required, MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
