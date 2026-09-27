using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Auth
{
    public class LoginRequest
    {
        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        // Captcha from GET /auth/captcha: the id identifies the server-side answer, the
        // answer is what the user typed. Required when Captcha:Enabled is true (default).
        public string? CaptchaId { get; set; }

        public string? CaptchaAnswer { get; set; }
    }
}
