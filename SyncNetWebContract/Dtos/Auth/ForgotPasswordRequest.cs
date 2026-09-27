using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Auth
{
    public class ForgotPasswordRequest
    {
        /// <summary>Either the account's user_name or its email — resolved server-side.</summary>
        [Required]
        public string Identifier { get; set; } = string.Empty;
    }
}
