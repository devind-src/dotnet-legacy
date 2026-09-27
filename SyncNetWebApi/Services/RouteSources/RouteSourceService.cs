using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.RouteSources;
using SyncNetApi.Dtos.Routing;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.RouteSources
{
    public class RouteSourceService : IRouteSourceService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public RouteSourceService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<RouteSourceDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.RoutesBySource.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(r => (r.notes ?? "").ToLower().Contains(f) || (r.inst_id ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(r => r.id).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<RouteSourceDto?> GetByIdAsync(int id)
        {
            var entity = await _context.RoutesBySource.AsNoTracking().FirstOrDefaultAsync(r => r.id == id);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<RouteSourceDto> CreateAsync(CreateRouteSourceRequest request, string actingUser)
        {
            if (!await _context.Nodes.AsNoTracking().AnyAsync(n => n.node_id == request.NodeIdIn))
                throw new ValidationException($"Source node '{request.NodeIdIn}' does not exist.");

            if (!await _context.Nodes.AsNoTracking().AnyAsync(n => n.node_id == request.NodeIdOut))
                throw new ValidationException($"Destination node '{request.NodeIdOut}' does not exist.");

            await EnsureNoFailoverAsync(request.InstId);

            var entity = new SwRoutesBySource
            {
                node_id_in = request.NodeIdIn,
                node_id_out = request.NodeIdOut,
                inst_id = request.InstId,
                notes = request.Notes
            };

            _context.RoutesBySource.Add(entity);
            _audit.LogInsert(entity, "sw_routes_by_source", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<RouteSourceDto> UpdateAsync(int id, UpdateRouteSourceRequest request, string actingUser)
        {
            var entity = await _context.RoutesBySource.FirstOrDefaultAsync(r => r.id == id)
                ?? throw new NotFoundException($"Route by source '{id}' not found.");

            if (request.InstId != entity.inst_id)
                await EnsureNoFailoverAsync(request.InstId);

            var before = ToDto(entity);

            entity.inst_id = request.InstId;
            entity.notes = request.Notes;

            _audit.LogUpdate(before, ToDto(entity), "sw_routes_by_source", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(int id, string actingUser)
        {
            var entity = await _context.RoutesBySource.FirstOrDefaultAsync(r => r.id == id)
                ?? throw new NotFoundException($"Route by source '{id}' not found.");

            _context.RoutesBySource.Remove(entity);
            _audit.LogDelete(entity, "sw_routes_by_source", actingUser);

            await _context.SaveChangesAsync();
        }

        // rule by source dikalahkan push route (sink_node) pada routing mode dynamic, jadi rule baru
        // untuk produk bermode dynamic ditolak. rule default (tanpa produk) tidak diperiksa di sini.
        private async Task EnsureNoFailoverAsync(string? instId)
        {
            if (string.IsNullOrWhiteSpace(instId)) return;

            string[] dynamicModes = [RoutingModes.Priority, RoutingModes.BestPrice, RoutingModes.LoadBalance];

            bool dynamicOn =
                await _context.RoutesMargin.AsNoTracking().AnyAsync(r => r.inst_id == instId && dynamicModes.Contains(r.routing_mode)) ||
                await _context.Fees.AsNoTracking().AnyAsync(f => f.product_id == instId && f.routing_mode != null && dynamicModes.Contains(f.routing_mode));

            if (dynamicOn)
                throw new ValidationException(
                    $"Produk '{instId}' memakai routing mode dynamic. Rule by source tidak berlaku selama mode dynamic, ubah routing mode produk itu ke Static dulu.");
        }

        private static RouteSourceDto ToDto(SwRoutesBySource e) => new(e.id, e.node_id_in, e.node_id_out, e.inst_id, e.notes);
    }
}
