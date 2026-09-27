using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.TerminalImages
{
    public class UpdateTerminalImageRequest
    {
        [MaxLength(255)]
        public string? UrlBase { get; set; }
    }
}
