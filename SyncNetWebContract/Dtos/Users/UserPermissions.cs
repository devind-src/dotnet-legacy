namespace SyncNetApi.Dtos.Users
{
    /// <summary>Resolved (not raw) add/edit/delete rights for one user — see
    /// IUserService.GetEffectivePermissionsAsync for how user-level overrides and the
    /// role's flag_add/edit/delete combine into this.</summary>
    public record UserPermissions(bool CanAdd, bool CanEdit, bool CanDelete);
}
