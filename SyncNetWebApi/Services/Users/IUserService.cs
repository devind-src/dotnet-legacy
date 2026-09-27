using System.Collections.Generic;
using System.Threading.Tasks;
using SyncNetApi.Dtos.Auth;
using SyncNetApi.Dtos.Users;
using SyncNetApi.Entities;

namespace SyncNetApi.Services.Users
{
    public interface IUserService
    {
        Task<DashboardUsers?> GetEntityByUserNameAsync(string userName);

        /// <summary>Case-insensitive exact match. Relies on the partial unique index on
        /// dashboard_user.email (see SyncNetDbContext) — without it this could return an
        /// arbitrary match among duplicates.</summary>
        Task<DashboardUsers?> GetEntityByEmailAsync(string email);
        Task<UserDto?> GetByUserNameAsync(string userName);
        Task<IReadOnlyList<UserDto>> GetRecordsAsync(string? filter);

        Task<UserDto> CreateAsync(CreateUserRequest request, string actingUser);
        Task<UserDto> UpdateAsync(string userName, UpdateUserRequest request, string actingUser);
        Task DeleteAsync(string userName, string actingUser);

        /// <summary>Resolves add/edit/delete rights: a user's own allow_add/edit/delete
        /// wins when set (not null), otherwise falls back to their role's current
        /// flag_add/edit/delete. Throws NotFoundException if userName doesn't exist.</summary>
        Task<UserPermissions> GetEffectivePermissionsAsync(string userName);

        Task ChangePasswordAsync(string userName, string oldPassword, string newPassword);

        /// <summary>Sets a new password outside the normal old-password-required flow.
        /// Used by: admin-initiated reset (mustChangePassword: true — forces the user to set
        /// their own password before anything else works), self-service forgot-password
        /// completion, and the forced-change-on-login challenge (both mustChangePassword:
        /// false — the user just deliberately chose this password).</summary>
        Task ResetPasswordAsync(string userName, string newPassword, string actingUser, bool mustChangePassword);

        Task RegisterFailedLoginAsync(string userName);
        Task ResetRetryAsync(string userName);
        Task MarkLoginSuccessAsync(string userName);

        Task<List<MenuItemDto>> GetMenuAsync(string roleName);

        Task LogActivityAsync(string userName, string hostAddress, string hostName, string userAgent, string state);
    }
}
