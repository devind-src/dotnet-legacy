using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Roles
{
    /// <summary>Full replace, like UpdateUserRequest — RoleName is the key and isn't editable here.</summary>
    public class UpdateRoleRequest
    {
        [Required, MaxLength(50)]
        public string RoleDesc { get; set; } = string.Empty;

        public bool FlagAdd { get; set; }
        public bool FlagEdit { get; set; }
        public bool FlagDelete { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty;

        public List<int> MenuIds { get; set; } = new();
    }
}
