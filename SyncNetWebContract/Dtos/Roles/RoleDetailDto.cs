using System.Collections.Generic;

namespace SyncNetApi.Dtos.Roles
{
    /// <summary>RoleDto plus the menu_ids currently assigned to this role via
    /// dashboard_role_menu — backs the Role edit form's menu checklist.</summary>
    public record RoleDetailDto(
        string RoleName,
        string RoleDesc,
        bool FlagAdd,
        bool FlagEdit,
        bool FlagDelete,
        string Status,
        List<int> MenuIds);
}
