using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.TerminalClients
{
    public class CreateTerminalClientRequest
    {
        [Required, MaxLength(30)]
        public string ClientId { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string ClientName { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? Username { get; set; }

        [MaxLength(50)]
        public string? Password { get; set; }

        [MaxLength(100)]
        public string? SecretId { get; set; }

        [MaxLength(100)]
        public string? SecretKey { get; set; }

        [MaxLength(100)]
        public string? MasterKey { get; set; }

        [MaxLength(100)]
        public string? SessionKey { get; set; }

        [MaxLength(15)]
        public string? GroupName { get; set; }

        [MaxLength(15)]
        public string? SubgroupName { get; set; }

        [MaxLength(1000)]
        public string? CallbackUrl { get; set; }

        [MaxLength(100)]
        public string? CallbackClientId { get; set; }

        [MaxLength(100)]
        public string? CallbackSecretKey { get; set; }
    }
}
