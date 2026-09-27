namespace SyncNetApi.Dtos.Roles
{
    /// <summary>List/dropdown shape — also backs the Role dropdown in the User form (only
    /// RoleName/RoleDesc are read there), so adding fields here is safe.</summary>
    public record RoleDto(string RoleName, string RoleDesc, bool FlagAdd, bool FlagEdit, bool FlagDelete, string Status);
}
