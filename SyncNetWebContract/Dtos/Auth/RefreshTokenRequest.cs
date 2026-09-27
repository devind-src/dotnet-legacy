using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Auth
{
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
