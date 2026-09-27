using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.Roles;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.Roles
{
    public class RoleService : IRoleService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public RoleService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<RoleDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.Roles.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(r =>
                    r.role_name.ToLower().Contains(f) ||
                    (r.role_desc ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(r => r.role_name).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<RoleDetailDto?> GetDetailAsync(string roleName)
        {
            var entity = await _context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.role_name == roleName);
            if (entity == null) return null;

            var menuIds = await _context.RoleMenu.AsNoTracking()
                .Where(rm => rm.role_name == roleName && rm.menu_id != null)
                .Select(rm => rm.menu_id!.Value)
                .ToListAsync();

            return ToDetailDto(entity, menuIds);
        }

        public async Task<RoleDetailDto> CreateAsync(CreateRoleRequest request, string actingUser)
        {
            var exists = await _context.Roles.AsNoTracking().AnyAsync(r => r.role_name == request.RoleName);
            if (exists)
                throw new ConflictException($"Role '{request.RoleName}' already exists.");

            var entity = new DashboardRoles
            {
                role_name = request.RoleName,
                role_desc = request.RoleDesc,
                flag_add = request.FlagAdd ? "1" : "0",
                flag_edit = request.FlagEdit ? "1" : "0",
                flag_delete = request.FlagDelete ? "1" : "0",
                status = "1"
            };

            _context.Roles.Add(entity);
            _audit.LogInsert(new { entity.role_name, entity.role_desc, entity.flag_add, entity.flag_edit, entity.flag_delete }, "dashboard_role", actingUser);

            var roleMenus = BuildRoleMenuRows(request.RoleName, request.MenuIds);
            _context.RoleMenu.AddRange(roleMenus);
            _audit.LogInsert(new { role_name = request.RoleName, menu_ids = request.MenuIds }, "dashboard_role_menu", actingUser);

            await _context.SaveChangesAsync();
            return ToDetailDto(entity, request.MenuIds);
        }

        public async Task<RoleDetailDto> UpdateAsync(string roleName, UpdateRoleRequest request, string actingUser)
        {
            var entity = await _context.Roles.FirstOrDefaultAsync(r => r.role_name == roleName)
                ?? throw new NotFoundException($"Role '{roleName}' not found.");

            var before = new { entity.role_desc, entity.flag_add, entity.flag_edit, entity.flag_delete, entity.status };

            entity.role_desc = request.RoleDesc;
            entity.flag_add = request.FlagAdd ? "1" : "0";
            entity.flag_edit = request.FlagEdit ? "1" : "0";
            entity.flag_delete = request.FlagDelete ? "1" : "0";
            entity.status = request.Status;

            _audit.LogUpdate(before, new { entity.role_desc, entity.flag_add, entity.flag_edit, entity.flag_delete, entity.status }, "dashboard_role", actingUser);

            var oldMenus = await _context.RoleMenu.Where(rm => rm.role_name == roleName).ToListAsync();
            _context.RoleMenu.RemoveRange(oldMenus);

            var newMenus = BuildRoleMenuRows(roleName, request.MenuIds);
            _context.RoleMenu.AddRange(newMenus);
            _audit.LogUpdate(
                new { role_name = roleName, menu_ids = oldMenus.Select(m => m.menu_id) },
                new { role_name = roleName, menu_ids = request.MenuIds },
                "dashboard_role_menu", actingUser);

            await _context.SaveChangesAsync();
            return ToDetailDto(entity, request.MenuIds);
        }

        public async Task DeleteAsync(string roleName, string actingUser)
        {
            var entity = await _context.Roles.FirstOrDefaultAsync(r => r.role_name == roleName)
                ?? throw new NotFoundException($"Role '{roleName}' not found.");

            var inUse = await _context.Users.AsNoTracking().AnyAsync(u => u.role_name == roleName);
            if (inUse)
                throw new ConflictException($"Role '{roleName}' is still assigned to one or more users and cannot be deleted.");

            _context.Roles.Remove(entity);
            _audit.LogDelete(new { entity.role_name, entity.role_desc }, "dashboard_role", actingUser);

            var roleMenus = await _context.RoleMenu.Where(rm => rm.role_name == roleName).ToListAsync();
            if (roleMenus.Count > 0)
            {
                _context.RoleMenu.RemoveRange(roleMenus);
                _audit.LogDelete(new { role_name = roleName, menu_ids = roleMenus.Select(m => m.menu_id) }, "dashboard_role_menu", actingUser);
            }

            await _context.SaveChangesAsync();
        }

        private static List<DashboardRoleMenu> BuildRoleMenuRows(string roleName, IReadOnlyCollection<int> menuIds) =>
            menuIds.Distinct().Select(menuId => new DashboardRoleMenu { role_name = roleName, menu_id = menuId }).ToList();

        private static RoleDto ToDto(DashboardRoles e) => new(
            e.role_name,
            e.role_desc ?? string.Empty,
            e.flag_add == "1",
            e.flag_edit == "1",
            e.flag_delete == "1",
            e.status ?? string.Empty);

        private static RoleDetailDto ToDetailDto(DashboardRoles e, List<int> menuIds) => new(
            e.role_name,
            e.role_desc ?? string.Empty,
            e.flag_add == "1",
            e.flag_edit == "1",
            e.flag_delete == "1",
            e.status ?? string.Empty,
            menuIds);
    }
}
