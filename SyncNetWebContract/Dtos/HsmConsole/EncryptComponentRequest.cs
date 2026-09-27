using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.HsmConsole
{
    public class EncryptComponentRequest
    {
        [Required]
        public string ClearComponent { get; set; } = string.Empty;
    }
}
