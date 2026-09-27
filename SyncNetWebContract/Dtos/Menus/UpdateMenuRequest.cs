using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Menus
{
    public class UpdateMenuRequest
    {
        [Required]
        public string Level1 { get; set; } = string.Empty;

        [Required]
        public string Level2 { get; set; } = string.Empty;

        public string? Level3 { get; set; }
        public string? Level4 { get; set; }
        public string? Icon { get; set; }

        [Required]
        public string Url { get; set; } = string.Empty;
    }
}
