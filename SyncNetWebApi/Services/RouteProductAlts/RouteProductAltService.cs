using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.RouteProductAlts;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;
using SyncNetApi.Services.RouteRules;

namespace SyncNetApi.Services.RouteProductAlts
{
    /// <summary>Biller alternate untuk produk bill payment (model primary-backup, bukan load
    /// balancer). Primary ada di `sw_routes_by_inst`; alternate berperingkat mulai 2.</summary>
    public class RouteProductAltService : IRouteProductAltService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public RouteProductAltService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<RouteProductAltDto>> GetByProductAsync(string instId)
        {
            var list = await _context.RoutesByInstAlt.AsNoTracking()
                .Where(a => a.inst_id == instId)
                .OrderBy(a => a.priority)
                .ToListAsync();

            return list.Select(ToDto).ToList();
        }

        public async Task<RouteProductAltDto?> GetByIdAsync(int id)
        {
            var entity = await _context.RoutesByInstAlt.AsNoTracking().FirstOrDefaultAsync(a => a.id == id);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<RouteProductAltDto> CreateAsync(CreateRouteProductAltRequest request, string actingUser)
        {
            var primary = await _context.RoutesByInst.FirstOrDefaultAsync(r => r.inst_id == request.InstId)
                ?? throw new ValidationException(
                    $"Produk '{request.InstId}' belum punya primary di Routing > Product.");

            await RouteCategoryRules.EnsureBillPaymentAsync(_context, request.InstId);
            await ValidateNodeAsync(request.NodeId, primary, request.InstId, ignoreAltId: null);

            int priority = request.Priority
                ?? (await _context.RoutesByInstAlt.AsNoTracking()
                        .Where(a => a.inst_id == request.InstId)
                        .Select(a => (int?)a.priority)
                        .MaxAsync() ?? 1) + 1;

            await ValidatePriorityAsync(request.InstId, priority, ignoreAltId: null);

            var now = LocalClock.Now;
            var entity = new SwRoutesByInstAlt
            {
                inst_id = request.InstId,
                node_id = request.NodeId,
                priority = (short)priority,
                notes = request.Notes,
                status = request.Active ? "1" : "0",
                created_by = actingUser,
                created_dt = now,
                updated_by = actingUser,
                updated_dt = now
            };

            _context.RoutesByInstAlt.Add(entity);
            _audit.LogInsert(entity, "sw_routes_by_inst_alt", actingUser);

            await _context.SaveChangesAsync();

            return ToDto(entity);
        }

        public async Task<RouteProductAltDto> UpdateAsync(int id, UpdateRouteProductAltRequest request, string actingUser)
        {
            var entity = await _context.RoutesByInstAlt.FirstOrDefaultAsync(a => a.id == id)
                ?? throw new NotFoundException($"Alternate '{id}' not found.");

            var primary = await _context.RoutesByInst.FirstOrDefaultAsync(r => r.inst_id == entity.inst_id)
                ?? throw new ValidationException($"Produk '{entity.inst_id}' belum punya primary di Routing > Product.");

            await ValidateNodeAsync(request.NodeId, primary, entity.inst_id, ignoreAltId: id);
            await ValidatePriorityAsync(entity.inst_id, request.Priority, ignoreAltId: id);

            var before = ToDto(entity);

            entity.node_id = request.NodeId;
            entity.priority = (short)request.Priority;
            entity.notes = request.Notes;
            entity.status = request.Active ? "1" : "0";
            entity.updated_by = actingUser;
            entity.updated_dt = LocalClock.Now;

            _audit.LogUpdate(before, ToDto(entity), "sw_routes_by_inst_alt", actingUser);

            await _context.SaveChangesAsync();

            return ToDto(entity);
        }

        public async Task DeleteAsync(int id, string actingUser)
        {
            var entity = await _context.RoutesByInstAlt.FirstOrDefaultAsync(a => a.id == id)
                ?? throw new NotFoundException($"Alternate '{id}' not found.");

            _context.RoutesByInstAlt.Remove(entity);
            _audit.LogDelete(entity, "sw_routes_by_inst_alt", actingUser);

            await _context.SaveChangesAsync();
        }

        private async Task ValidateNodeAsync(int nodeId, SwRoutesByInst primary, string instId, int? ignoreAltId)
        {
            if (!await _context.Nodes.AsNoTracking().AnyAsync(n => n.node_id == nodeId))
                throw new ValidationException($"Node '{nodeId}' does not exist.");

            if (nodeId == primary.node_id)
                throw new ValidationException("Node itu adalah primary produk ini. Pilih biller lain sebagai alternate.");

            if (await _context.RoutesByInstAlt.AsNoTracking()
                    .AnyAsync(a => a.inst_id == instId && a.node_id == nodeId && a.id != (ignoreAltId ?? -1)))
                throw new ConflictException("Node itu sudah menjadi alternate produk ini.");
        }

        private async Task ValidatePriorityAsync(string instId, int priority, int? ignoreAltId)
        {
            if (priority < 2)
                throw new ValidationException("Priority minimal 2 (urutan 1 adalah primary).");

            if (await _context.RoutesByInstAlt.AsNoTracking()
                    .AnyAsync(a => a.inst_id == instId && a.priority == priority && a.id != (ignoreAltId ?? -1)))
                throw new ConflictException($"Priority {priority} sudah dipakai alternate lain untuk produk ini.");
        }

        private static RouteProductAltDto ToDto(SwRoutesByInstAlt e)
            => new(e.id, e.inst_id, e.node_id, e.priority, e.notes, e.status == "1");
    }
}
