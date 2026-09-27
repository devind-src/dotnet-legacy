using SyncNetApi.Dtos.Auth;
using SyncNetApi.Dtos.Users;

namespace SyncNetWasm.Services
{
    /// <summary>Everything from LoginResponse besides the tokens themselves (those live in
    /// TokenStorageService) — the current user's profile, nav menu, and effective
    /// permissions. Populated by AuthService on login/refresh, cleared on logout.</summary>
    public class SessionState
    {
        public string? UserName { get; private set; }
        public string? FullName { get; private set; }
        public string? RoleName { get; private set; }
        public List<MenuItemDto> Menus { get; private set; } = [];
        public UserPermissions UserManagementPermissions { get; private set; } = new(false, false, false);

        public event Action? Changed;

        public void Set(LoginResponse login)
        {
            UserName = login.UserName;
            FullName = login.FullName;
            RoleName = login.RoleName;
            Menus = login.Menus;
            UserManagementPermissions = login.UserManagementPermissions;
            Changed?.Invoke();
        }

        public void Clear()
        {
            UserName = null;
            FullName = null;
            RoleName = null;
            Menus = [];
            UserManagementPermissions = new(false, false, false);
            Changed?.Invoke();
        }
    }
}
