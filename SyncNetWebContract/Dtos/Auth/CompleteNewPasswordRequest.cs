using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Auth
{
    /// <summary>Completes the NEW_PASSWORD_REQUIRED login challenge (see LoginChallengeResponse)
    /// — issued when must_change_password is set (e.g. after an admin reset).</summary>
    public class CompleteNewPasswordRequest
    {
        [Required]
        public string UserName { get; set; } = string.Empty;

        /// <summary>The password the user just authenticated with at /auth/login — re-verified
        /// here since this endpoint is anonymous (no session exists yet to prove it).</summary>
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required, MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
