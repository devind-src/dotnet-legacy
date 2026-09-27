using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.Menus;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.Menus
{
    public class MenuService : IMenuService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public MenuService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<MenuDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.Menus.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();

                if (int.TryParse(f, out var menuId))
                {
                    query = query.Where(m => m.menu_id == menuId);
                }
                else
                {
                    query = query.Where(m =>
                        (m.level_1 ?? "").ToLower().Contains(f) ||
                        (m.level_2 ?? "").ToLower().Contains(f) ||
                        (m.level_3 ?? "").ToLower().Contains(f) ||
                        (m.level_4 ?? "").ToLower().Contains(f) ||
                        (m.icon ?? "").ToLower().Contains(f) ||
                        (m.url ?? "").ToLower().Contains(f));
                }
            }

            var list = await query.OrderBy(m => m.menu_id).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<MenuDto?> GetByIdAsync(int menuId)
        {
            var entity = await _context.Menus.AsNoTracking().FirstOrDefaultAsync(m => m.menu_id == menuId);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<MenuDto> CreateAsync(CreateMenuRequest request, string actingUser)
        {
            var exists = await _context.Menus.AsNoTracking().AnyAsync(m => m.menu_id == request.MenuId);
            if (exists)
                throw new ConflictException($"Menu id '{request.MenuId}' already exists.");

            var entity = new DashboardMenus
            {
                menu_id = request.MenuId,
                level_1 = request.Level1,
                level_2 = request.Level2,
                level_3 = request.Level3,
                level_4 = request.Level4,
                icon = request.Icon,
                url = request.Url
            };

            _context.Menus.Add(entity);
            _audit.LogInsert(new { entity.menu_id, entity.level_1, entity.level_2, entity.level_3, entity.level_4, entity.url }, "dashboard_menu", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<MenuDto> UpdateAsync(int menuId, UpdateMenuRequest request, string actingUser)
        {
            var entity = await _context.Menus.FirstOrDefaultAsync(m => m.menu_id == menuId)
                ?? throw new NotFoundException($"Menu '{menuId}' not found.");

            var before = new { entity.level_1, entity.level_2, entity.level_3, entity.level_4, entity.icon, entity.url };

            entity.level_1 = request.Level1;
            entity.level_2 = request.Level2;
            entity.level_3 = request.Level3;
            entity.level_4 = request.Level4;
            entity.icon = request.Icon;
            entity.url = request.Url;

            _audit.LogUpdate(before, new { entity.level_1, entity.level_2, entity.level_3, entity.level_4, entity.icon, entity.url }, "dashboard_menu", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(int menuId, string actingUser)
        {
            var entity = await _context.Menus.FirstOrDefaultAsync(m => m.menu_id == menuId)
                ?? throw new NotFoundException($"Menu '{menuId}' not found.");

            _context.Menus.Remove(entity);
            _audit.LogDelete(new { entity.menu_id, entity.level_1, entity.level_2 }, "dashboard_menu", actingUser);

            // Improvement over legacy MenuDelete: also drop role_menu rows that pointed at
            // this menu, so we never leave dashboard_role_menu with an orphan menu_id.
            var roleMenus = await _context.RoleMenu.Where(rm => rm.menu_id == menuId).ToListAsync();
            if (roleMenus.Count > 0)
            {
                _context.RoleMenu.RemoveRange(roleMenus);
                _audit.LogDelete(new { menu_id = menuId, role_names = roleMenus.Select(m => m.role_name) }, "dashboard_role_menu", actingUser);
            }

            await _context.SaveChangesAsync();
        }

        private static MenuDto ToDto(DashboardMenus e) => new(
            e.menu_id,
            e.level_1 ?? string.Empty,
            e.level_2 ?? string.Empty,
            e.level_3 ?? string.Empty,
            e.level_4 ?? string.Empty,
            e.icon ?? string.Empty,
            e.url ?? string.Empty);
    }
}
