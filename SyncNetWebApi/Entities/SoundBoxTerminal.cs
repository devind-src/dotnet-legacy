using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    [Table("soundbox_terminal", Schema = "public")]
    public class SoundBoxTerminal
    {
        [Key]
        public string nmid { get; set; } = string.Empty;
        public string? store_name { get; set; }
        public string? address { get; set; }
        public string? city { get; set; }
        public string? serial_number { get; set; }
        public string? provider { get; set; }
        public string? group_name { get; set; }
        public string? subgroup_name { get; set; }
    }
}
