using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    [Table("dashboard_menu", Schema = "public")]
    public class DashboardMenus
    {
        [Key]
        public int menu_id { get; set; }

        // Nullable to match the real table (level_3/level_4 are legitimately NULL for menu
        // rows that don't go 3/4 levels deep — most of them, in practice: 17 and 51 of 92
        // rows respectively). Mapping these non-nullable made GetMenuAsync throw
        // InvalidCastException for every single role, i.e. login was broken for everyone.
        public string? level_1 { get; set; }
        public string? level_2 { get; set; }
        public string? level_3 { get; set; }
        public string? level_4 { get; set; }
        public string? icon { get; set; }
        public string? url { get; set; }
    }
}
