using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.RouteDynamics;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.RouteDynamics
{
    public class RouteDynamicService : IRouteDynamicService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public RouteDynamicService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<RouteDynamicDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.RoutesDynamic.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(r => r.inst_id.ToLower().Contains(f) || (r.notes ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(r => r.inst_id).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<RouteDynamicDto?> GetByIdAsync(string instId)
        {
            var entity = await _context.RoutesDynamic.AsNoTracking().FirstOrDefaultAsync(r => r.inst_id == instId);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<RouteDynamicDto> CreateAsync(CreateRouteDynamicRequest request, string actingUser)
        {
            if (await _context.RoutesDynamic.AsNoTracking().AnyAsync(r => r.inst_id == request.InstId))
                throw new ConflictException($"Product '{request.InstId}' already has a dynamic route.");

            var entity = new SwRoutesDynamic
            {
                inst_id = request.InstId,
                notes = request.Notes
            };

            _context.RoutesDynamic.Add(entity);
            _audit.LogInsert(entity, "sw_routes_dynamic", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<RouteDynamicDto> UpdateAsync(string instId, UpdateRouteDynamicRequest request, string actingUser)
        {
            var entity = await _context.RoutesDynamic.FirstOrDefaultAsync(r => r.inst_id == instId)
                ?? throw new NotFoundException($"Route dynamic '{instId}' not found.");

            var before = ToDto(entity);
            entity.notes = request.Notes;

            _audit.LogUpdate(before, ToDto(entity), "sw_routes_dynamic", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(string instId, string actingUser)
        {
            var entity = await _context.RoutesDynamic.FirstOrDefaultAsync(r => r.inst_id == instId)
                ?? throw new NotFoundException($"Route dynamic '{instId}' not found.");

            _context.RoutesDynamic.Remove(entity);
            _audit.LogDelete(entity, "sw_routes_dynamic", actingUser);

            await _context.SaveChangesAsync();
        }

        private static RouteDynamicDto ToDto(SwRoutesDynamic e) => new(e.inst_id, e.notes);
    }
}
