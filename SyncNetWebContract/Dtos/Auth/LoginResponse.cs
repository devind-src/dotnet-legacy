using System;
using System.Collections.Generic;
using SyncNetApi.Dtos.Users;

namespace SyncNetApi.Dtos.Auth
{
    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiresAt { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiresAt { get; set; }

        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public List<MenuItemDto> Menus { get; set; } = [];

        /// <summary>The caller's own effective add/edit/delete rights for the User
        /// Management module (see IUserService.GetEffectivePermissionsAsync) — lets the UI
        /// hide/disable actions up front instead of discovering a 403 after the fact.
        /// Future modules with their own permission sets would get their own field here
        /// rather than overloading this one.</summary>
        public UserPermissions UserManagementPermissions { get; set; } = new(false, false, false);
    }

    public class MenuItemDto
    {
        public int MenuId { get; set; }
        public string Level1 { get; set; } = string.Empty;
        public string Level2 { get; set; } = string.Empty;
        public string Level3 { get; set; } = string.Empty;
        public string Level4 { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }
}
