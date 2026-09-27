using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    [Table("sw_terminal_image", Schema = "public")]
    public class SwTerminalImage
    {
        [Key]
        public string name { get; set; } = string.Empty;
        public string? url_base { get; set; }
        public string? logo { get; set; }
        public string? banner_1 { get; set; }
        public string? banner_2 { get; set; }
        public string? banner_3 { get; set; }
        public string? banner_4 { get; set; }
        public string? banner_5 { get; set; }
    }
}
