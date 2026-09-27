using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.Auth;
using SyncNetApi.Dtos.Users;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;
using SyncNetApi.Services.Auth;

namespace SyncNetApi.Services.Users
{
    public class UserService : IUserService
    {
        private readonly SyncNetDbContext _context;
        private readonly IPasswordService _passwordService;
        private readonly IAuditService _audit;

        public UserService(SyncNetDbContext context, IPasswordService passwordService, IAuditService audit)
        {
            _context = context;
            _passwordService = passwordService;
            _audit = audit;
        }

        public Task<DashboardUsers?> GetEntityByUserNameAsync(string userName) =>
            _context.Users.FirstOrDefaultAsync(x => x.user_name == userName);

        public Task<DashboardUsers?> GetEntityByEmailAsync(string email) =>
            _context.Users.FirstOrDefaultAsync(x => x.email != null && x.email.ToLower() == email.ToLower());

        public async Task<UserDto?> GetByUserNameAsync(string userName)
        {
            var entity = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.user_name == userName);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<IReadOnlyList<UserDto>> GetRecordsAsync(string? filter)
        {
            var query = _context.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(u =>
                    u.user_name.ToLower().Contains(f) ||
                    (u.full_name ?? "").ToLower().Contains(f) ||
                    (u.role_name ?? "").ToLower().Contains(f) ||
                    (u.email ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(u => u.user_name).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<UserDto> CreateAsync(CreateUserRequest request, string actingUser)
        {
            var exists = await _context.Users.AsNoTracking().AnyAsync(x => x.user_name == request.UserName);
            if (exists)
                throw new ConflictException($"User '{request.UserName}' already exists.");

            // dashboard_user.email has a case-insensitive unique index (see SyncNetDbContext) —
            // pre-check so a duplicate email gets a clean 409 instead of an unhandled Postgres
            // unique-violation surfacing as a generic 500.
            var emailTaken = await _context.Users.AsNoTracking()
                .AnyAsync(x => x.email != null && x.email.ToLower() == request.Email.ToLower());
            if (emailTaken)
                throw new ConflictException($"Email '{request.Email}' is already in use.");

            var entity = new DashboardUsers
            {
                user_name = request.UserName,
                full_name = request.FullName,
                role_name = request.RoleName,
                email = request.Email,
                password = _passwordService.Hash(request.Password),
                status = "1",
                allow_add = request.AllowAdd,
                allow_edit = request.AllowEdit,
                allow_delete = request.AllowDelete
            };

            _context.Users.Add(entity);
            _audit.LogInsert(new { entity.user_name, entity.full_name, entity.role_name, entity.email }, "dashboard_user", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<UserDto> UpdateAsync(string userName, UpdateUserRequest request, string actingUser)
        {
            var entity = await _context.Users.FirstOrDefaultAsync(x => x.user_name == userName)
                ?? throw new NotFoundException($"User '{userName}' not found.");

            var wasActiveAdmin = entity.role_name == "admin" && entity.status == "1";
            var staysActiveAdmin = request.RoleName == "admin" && request.Status == "1";
            if (wasActiveAdmin && !staysActiveAdmin && await IsLastActiveAdminAsync(userName))
                throw new ValidationException("Cannot deactivate or reassign the last active admin account.");

            var before = new { entity.full_name, entity.role_name, entity.email, entity.status };

            entity.full_name = request.FullName;
            entity.role_name = request.RoleName;
            entity.email = request.Email;
            entity.status = request.Status;
            entity.allow_add = request.AllowAdd;
            entity.allow_edit = request.AllowEdit;
            entity.allow_delete = request.AllowDelete;

            _audit.LogUpdate(before, new { entity.full_name, entity.role_name, entity.email, entity.status }, "dashboard_user", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<UserPermissions> GetEffectivePermissionsAsync(string userName)
        {
            var entity = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.user_name == userName)
                ?? throw new NotFoundException($"User '{userName}' not found.");

            var role = await _context.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.role_name == entity.role_name);

            return new UserPermissions(
                CanAdd: entity.allow_add ?? (role?.flag_add == "1"),
                CanEdit: entity.allow_edit ?? (role?.flag_edit == "1"),
                CanDelete: entity.allow_delete ?? (role?.flag_delete == "1"));
        }

        public async Task DeleteAsync(string userName, string actingUser)
        {
            if (userName == actingUser)
                throw new ValidationException("You cannot delete your own account.");

            var entity = await _context.Users.FirstOrDefaultAsync(x => x.user_name == userName)
                ?? throw new NotFoundException($"User '{userName}' not found.");

            if (entity.role_name == "admin" && entity.status == "1" && await IsLastActiveAdminAsync(userName))
                throw new ValidationException("Cannot delete the last active admin account.");

            _context.Users.Remove(entity);
            _audit.LogDelete(new { entity.user_name, entity.full_name }, "dashboard_user", actingUser);

            await _context.SaveChangesAsync();
        }

        public async Task ChangePasswordAsync(string userName, string oldPassword, string newPassword)
        {
            var entity = await _context.Users.FirstOrDefaultAsync(x => x.user_name == userName)
                ?? throw new NotFoundException($"User '{userName}' not found.");

            var verify = _passwordService.Verify(userName, oldPassword, entity.password ?? string.Empty);
            if (verify == PasswordVerifyResult.Failed)
                throw new InvalidCredentialsException("Old password does not match.");

            entity.password = _passwordService.Hash(newPassword);
            entity.must_change_password = false;
            _audit.LogUpdate(new { field = "password" }, new { field = "password" }, "dashboard_user", userName);

            await _context.SaveChangesAsync();
        }

        public async Task ResetPasswordAsync(string userName, string newPassword, string actingUser, bool mustChangePassword)
        {
            var entity = await _context.Users.FirstOrDefaultAsync(x => x.user_name == userName)
                ?? throw new NotFoundException($"User '{userName}' not found.");

            entity.password = _passwordService.Hash(newPassword);
            entity.retry = 0;
            entity.must_change_password = mustChangePassword;
            _audit.LogUpdate(new { field = "password", reset_by = actingUser }, new { field = "password" }, "dashboard_user", actingUser);

            await _context.SaveChangesAsync();
        }

        public async Task RegisterFailedLoginAsync(string userName)
        {
            var entity = await _context.Users.FirstOrDefaultAsync(x => x.user_name == userName);
            if (entity == null) return;

            entity.retry += 1;
            await _context.SaveChangesAsync();
        }

        public async Task ResetRetryAsync(string userName)
        {
            var entity = await _context.Users.FirstOrDefaultAsync(x => x.user_name == userName);
            if (entity == null) return;

            entity.retry = 0;
            await _context.SaveChangesAsync();
        }

        public async Task MarkLoginSuccessAsync(string userName)
        {
            var entity = await _context.Users.FirstOrDefaultAsync(x => x.user_name == userName);
            if (entity == null) return;

            entity.retry = 0;
            // Must be UTC: Npgsql's EF provider defaults an unannotated DateTime column to
            // timestamptz (regardless of what the real, pre-existing column type is — this
            // table was never created by our migrations) and rejects Kind=Local outright.
            entity.last_login = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<List<MenuItemDto>> GetMenuAsync(string roleName)
        {
            var query = from role in _context.Roles.AsNoTracking()
                        join rm in _context.RoleMenu.AsNoTracking() on role.role_name equals rm.role_name
                        join menu in _context.Menus.AsNoTracking() on rm.menu_id equals (int?)menu.menu_id
                        where role.role_name == roleName
                        orderby menu.menu_id
                        select new MenuItemDto
                        {
                            MenuId = menu.menu_id,
                            Level1 = menu.level_1 ?? string.Empty,
                            Level2 = menu.level_2 ?? string.Empty,
                            Level3 = menu.level_3 ?? string.Empty,
                            Level4 = menu.level_4 ?? string.Empty,
                            Icon = menu.icon ?? string.Empty,
                            Url = menu.url ?? string.Empty,
                            CanAdd = role.flag_add == "1",
                            CanEdit = role.flag_edit == "1",
                            CanDelete = role.flag_delete == "1"
                        };

            return await query.ToListAsync();
        }

        public async Task LogActivityAsync(string userName, string hostAddress, string hostName, string userAgent, string state)
        {
            _context.UserLog.Add(new DashboardUserLog
            {
                user_name = userName,
                // date_time is "timestamp without time zone" (explicitly annotated, §7.27) —
                // Npgsql rejects writing a Kind=Utc DateTime to that column type outright, so
                // the same UTC instant is re-tagged Unspecified here (not switched to
                // DateTime.Now — that would silently change stored values to local time and
                // break consistency with every row written before this fix).
                date_time = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
                host_address = hostAddress,
                host_name = hostName,
                host_agent = userAgent,
                state = state
            });

            await _context.SaveChangesAsync();
        }

        /// <summary>True if, excluding <paramref name="excludeUserName"/>, no other active
        /// (status "1") admin account exists. Used to block a Delete/Update that would leave
        /// the system with zero admins able to manage users.</summary>
        private async Task<bool> IsLastActiveAdminAsync(string excludeUserName)
        {
            var anyOtherActiveAdmin = await _context.Users.AsNoTracking()
                .AnyAsync(u => u.role_name == "admin" && u.status == "1" && u.user_name != excludeUserName);
            return !anyOtherActiveAdmin;
        }

        private static UserDto ToDto(DashboardUsers e) => new()
        {
            UserName = e.user_name,
            FullName = e.full_name ?? string.Empty,
            RoleName = e.role_name ?? string.Empty,
            Email = e.email ?? string.Empty,
            Status = e.status ?? string.Empty,
            Retry = e.retry,
            MaxRetry = e.max_retry ?? 3,
            LastLogin = e.last_login,
            MustChangePassword = e.must_change_password ?? false,
            AllowAdd = e.allow_add,
            AllowEdit = e.allow_edit,
            AllowDelete = e.allow_delete
        };
    }
}
