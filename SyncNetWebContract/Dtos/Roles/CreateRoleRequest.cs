using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Roles
{
    public class CreateRoleRequest
    {
        [Required, MaxLength(30)]
        public string RoleName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string RoleDesc { get; set; } = string.Empty;

        public bool FlagAdd { get; set; } = true;
        public bool FlagEdit { get; set; } = true;
        public bool FlagDelete { get; set; } = true;

        /// <summary>menu_id values assigned to this role — replaces dashboard_role_menu wholesale.</summary>
        public List<int> MenuIds { get; set; } = new();
    }
}
