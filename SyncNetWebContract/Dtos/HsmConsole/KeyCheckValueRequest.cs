using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.HsmConsole
{
    public class KeyCheckValueRequest
    {
        [Required]
        public string Component { get; set; } = string.Empty;
    }
}
