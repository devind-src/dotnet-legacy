using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    [Table("dashboard_role", Schema = "public")]
    public class DashboardRoles
    {
        [Key]
        public string role_name { get; set; } = string.Empty;
        public string? role_desc { get; set; }
        public string? flag_add { get; set; }
        public string? flag_edit { get; set; }
        public string? flag_delete { get; set; }
        public string? status { get; set; }
    }
}
