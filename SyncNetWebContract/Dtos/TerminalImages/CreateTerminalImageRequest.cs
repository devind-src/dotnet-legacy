using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.TerminalImages
{
    public class CreateTerminalImageRequest
    {
        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? UrlBase { get; set; }
    }
}
