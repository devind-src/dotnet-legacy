using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    [Table("dashboard_role_menu", Schema = "public")]
    public class DashboardRoleMenu
    {
        // id was manually computed as max(id)+1 in application code (see RoleService history)
        // because the real table originally had no DB-side sequence. Converted to a real
        // Postgres IDENTITY column — DB now assigns it on insert.
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }

        // NOTE: this is purely a role<->menu mapping table — flag_add/edit/delete columns
        // were previously declared here but never existed on the real table (GetMenuAsync
        // has always read those flags from DashboardRoles instead, so nothing broke, but the
        // dead properties were misleading). Nullable to match the real (nullable) columns.
        public string? role_name { get; set; }
        public int? menu_id { get; set; }
    }
}
