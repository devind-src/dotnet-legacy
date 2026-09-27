using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.RouteProducts;
using SyncNetApi.Dtos.Routing;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;
using SyncNetApi.Services.RouteRules;

namespace SyncNetApi.Services.RouteProducts
{
    public class RouteProductService : IRouteProductService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public RouteProductService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<RouteProductDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.RoutesByInst.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(r => r.inst_id.ToLower().Contains(f) || (r.notes ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(r => r.inst_id).ToListAsync();
            return await BuildDtosAsync(list);
        }

        public async Task<RouteProductDto?> GetByIdAsync(string instId)
        {
            var entity = await _context.RoutesByInst.AsNoTracking().FirstOrDefaultAsync(r => r.inst_id == instId);
            return entity == null ? null : (await BuildDtosAsync([entity])).Single();
        }

        public async Task<RouteProductDto> CreateAsync(CreateRouteProductRequest request, string actingUser)
        {
            // Routing > Product hanya untuk produk kategori bill payment
            await RouteCategoryRules.EnsureBillPaymentAsync(_context, request.InstId);

            if (!await _context.Nodes.AsNoTracking().AnyAsync(n => n.node_id == request.NodeId))
                throw new ValidationException($"Node '{request.NodeId}' does not exist.");

            if (await _context.RoutesByInst.AsNoTracking().AnyAsync(r => r.inst_id == request.InstId))
                throw new ConflictException($"Product '{request.InstId}' already has a routing rule.");

            var entity = new SwRoutesByInst
            {
                inst_id = request.InstId,
                node_id = request.NodeId,
                notes = request.Notes
            };

            _context.RoutesByInst.Add(entity);
            _audit.LogInsert(entity, "sw_routes_by_inst", actingUser);

            await _context.SaveChangesAsync();
            return (await BuildDtosAsync([entity])).Single();
        }

        public async Task<RouteProductDto> UpdateAsync(string instId, UpdateRouteProductRequest request, string actingUser)
        {
            var entity = await _context.RoutesByInst.FirstOrDefaultAsync(r => r.inst_id == instId)
                ?? throw new NotFoundException($"Route by product '{instId}' not found.");

            if (!await _context.Nodes.AsNoTracking().AnyAsync(n => n.node_id == request.NodeId))
                throw new ValidationException($"Node '{request.NodeId}' does not exist.");

            // primary tidak boleh sama dengan salah satu alternate produk ini
            if (request.NodeId != entity.node_id &&
                await _context.RoutesByInstAlt.AsNoTracking().AnyAsync(a => a.inst_id == instId && a.node_id == request.NodeId))
                throw new ValidationException(
                    "Node itu sudah menjadi biller alternate produk ini. Hapus dulu dari daftar alternate.");

            var before = (await BuildDtosAsync([entity])).Single();
            entity.node_id = request.NodeId;
            entity.notes = request.Notes;

            _audit.LogUpdate(before, (await BuildDtosAsync([entity])).Single(), "sw_routes_by_inst", actingUser);

            await _context.SaveChangesAsync();
            return (await BuildDtosAsync([entity])).Single();
        }

        public async Task DeleteAsync(string instId, string actingUser)
        {
            var entity = await _context.RoutesByInst.FirstOrDefaultAsync(r => r.inst_id == instId)
                ?? throw new NotFoundException($"Route by product '{instId}' not found.");

            // alternate ikut dihapus supaya tidak menjadi yatim
            var alternates = await _context.RoutesByInstAlt.Where(a => a.inst_id == instId).ToListAsync();
            foreach (var alt in alternates)
            {
                _context.RoutesByInstAlt.Remove(alt);
                _audit.LogDelete(alt, "sw_routes_by_inst_alt", actingUser);
            }

            _context.RoutesByInst.Remove(entity);
            _audit.LogDelete(entity, "sw_routes_by_inst", actingUser);

            await _context.SaveChangesAsync();
        }

        private async Task<List<RouteProductDto>> BuildDtosAsync(IReadOnlyCollection<SwRoutesByInst> entities)
        {
            var ids = entities.Select(e => e.inst_id).ToList();

            var alternates = await _context.RoutesByInstAlt.AsNoTracking()
                .Where(a => ids.Contains(a.inst_id) && a.status == "1")
                .OrderBy(a => a.priority)
                .ToListAsync();

            var sourceRules = (await _context.RoutesBySource.AsNoTracking()
                .Where(r => r.inst_id != null && ids.Contains(r.inst_id))
                .Select(r => r.inst_id!)
                .Distinct()
                .ToListAsync()).ToHashSet();

            // mode dari baris default Product Fees (diatur di Product > Fees > Product Fees)
            var modes = await _context.Fees.AsNoTracking()
                .Where(f => f.product_id != null && ids.Contains(f.product_id)
                    && (f.merchant_id == null || f.merchant_id == "")
                    && (f.submerchant_id == null || f.submerchant_id == ""))
                .Select(f => new { f.product_id, f.routing_mode })
                .ToListAsync();

            return entities.Select(e => new RouteProductDto(
                e.inst_id, e.node_id, e.notes,
                modes.FirstOrDefault(m => m.product_id == e.inst_id)?.routing_mode ?? RoutingModes.Static,
                alternates.Where(a => a.inst_id == e.inst_id).Select(a => a.node_id).ToList(),
                sourceRules.Contains(e.inst_id))).ToList();
        }
    }
}
