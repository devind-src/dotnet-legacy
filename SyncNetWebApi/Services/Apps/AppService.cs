using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.Apps;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.Apps
{
    public class AppService : IAppService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public AppService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<AppDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.Apps.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(a => a.app_name.ToLower().Contains(f) || (a.host ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(a => a.app_name).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<AppDto?> GetByIdAsync(string appName)
        {
            var entity = await _context.Apps.AsNoTracking().FirstOrDefaultAsync(a => a.app_name == appName);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<int> GetNewCommandPortAsync()
        {
            var port = await _context.Apps.AsNoTracking()
                .OrderByDescending(a => a.command_port)
                .Select(a => a.command_port)
                .FirstOrDefaultAsync();

            return string.IsNullOrEmpty(port) ? 41000 : int.Parse(port) + 1;
        }

        public async Task<AppDto> CreateAsync(CreateAppRequest request, string actingUser)
        {
            var exists = await _context.Apps.AsNoTracking().AnyAsync(a => a.app_name == request.AppName);
            if (exists)
                throw new ConflictException($"Application '{request.AppName}' already exists.");

            // Matches legacy AppPortExist guard — only checked on create.
            var portInUse = await _context.Apps.AsNoTracking().AnyAsync(a => a.command_port == request.CommandPort);
            if (portInUse)
                throw new ConflictException($"Command port '{request.CommandPort}' is already used by another application.");

            var entity = new SwApp
            {
                app_name = request.AppName,
                app_type = request.AppType,
                host = request.Host,
                command_port = request.CommandPort,
                status = "1",
                created_by = actingUser,
                created_dt = DateTime.UtcNow
            };

            _context.Apps.Add(entity);
            _audit.LogInsert(entity, "sw_app", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<AppDto> UpdateAsync(string appName, UpdateAppRequest request, string actingUser)
        {
            var entity = await _context.Apps.FirstOrDefaultAsync(a => a.app_name == appName)
                ?? throw new NotFoundException($"Application '{appName}' not found.");

            var before = new { entity.app_type, entity.host, entity.command_port, entity.status };
            entity.app_type = request.AppType;
            entity.host = request.Host;
            entity.command_port = request.CommandPort;
            entity.status = request.Active ? "1" : "0";
            entity.updated_by = actingUser;

            _audit.LogUpdate(before, new { entity.app_type, entity.host, entity.command_port, entity.status }, "sw_app", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(string appName, string actingUser)
        {
            var entity = await _context.Apps.FirstOrDefaultAsync(a => a.app_name == appName)
                ?? throw new NotFoundException($"Application '{appName}' not found.");

            _context.Apps.Remove(entity);
            _audit.LogDelete(new { entity.app_name }, "sw_app", actingUser);

            await _context.SaveChangesAsync();
        }

        private static AppDto ToDto(SwApp e) => new(e.app_name, e.host, e.app_type, e.command_port, e.status == "1", e.path, e.last_update);
    }
}
