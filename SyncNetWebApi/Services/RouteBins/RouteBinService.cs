using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.RouteBins;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.RouteBins
{
    public class RouteBinService : IRouteBinService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public RouteBinService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<RouteBinDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.RoutesByBin.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter) && int.TryParse(filter.Sanitize(), out var groupId))
                query = query.Where(r => r.group_id == groupId);

            var list = await query.OrderBy(r => r.group_id).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<RouteBinDto?> GetByIdAsync(int groupId)
        {
            var entity = await _context.RoutesByBin.AsNoTracking().FirstOrDefaultAsync(r => r.group_id == groupId);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<RouteBinDto> CreateAsync(CreateRouteBinRequest request, string actingUser)
        {
            if (!await _context.CardGroups.AsNoTracking().AnyAsync(g => g.group_id == request.GroupId))
                throw new ValidationException($"Card group '{request.GroupId}' does not exist.");

            if (!await _context.Nodes.AsNoTracking().AnyAsync(n => n.node_id == request.NodeId))
                throw new ValidationException($"Node '{request.NodeId}' does not exist.");

            if (await _context.RoutesByBin.AsNoTracking().AnyAsync(r => r.group_id == request.GroupId))
                throw new ConflictException($"Card group '{request.GroupId}' already has a BIN route.");

            var entity = new SwRoutesByBin
            {
                group_id = request.GroupId,
                node_id = request.NodeId
            };

            _context.RoutesByBin.Add(entity);
            _audit.LogInsert(entity, "sw_routes_by_bin", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<RouteBinDto> UpdateAsync(int groupId, UpdateRouteBinRequest request, string actingUser)
        {
            var entity = await _context.RoutesByBin.FirstOrDefaultAsync(r => r.group_id == groupId)
                ?? throw new NotFoundException($"Route by BIN '{groupId}' not found.");

            if (!await _context.Nodes.AsNoTracking().AnyAsync(n => n.node_id == request.NodeId))
                throw new ValidationException($"Node '{request.NodeId}' does not exist.");

            var before = ToDto(entity);
            entity.node_id = request.NodeId;

            _audit.LogUpdate(before, ToDto(entity), "sw_routes_by_bin", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(int groupId, string actingUser)
        {
            var entity = await _context.RoutesByBin.FirstOrDefaultAsync(r => r.group_id == groupId)
                ?? throw new NotFoundException($"Route by BIN '{groupId}' not found.");

            _context.RoutesByBin.Remove(entity);
            _audit.LogDelete(entity, "sw_routes_by_bin", actingUser);

            await _context.SaveChangesAsync();
        }

        private static RouteBinDto ToDto(SwRoutesByBin e) => new(e.group_id, e.node_id);
    }
}
