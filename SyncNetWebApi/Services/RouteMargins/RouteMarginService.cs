using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.RouteMargins;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;
using SyncNetApi.Services.RouteRules;

namespace SyncNetApi.Services.RouteMargins
{
    public class RouteMarginService : IRouteMarginService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public RouteMarginService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<RouteMarginDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.RoutesMargin.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(r => r.inst_id.ToLower().Contains(f) || (r.notes ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(r => r.inst_id).ToListAsync();
            var ids = list.Select(r => r.inst_id).ToList();

            var supplierCounts = await ActiveSupplierCountsAsync(ids);
            var sourceRules = await SourceRuleProductsAsync(ids);

            return list.Select(e => ToDto(e, supplierCounts, sourceRules)).ToList();
        }

        public async Task<RouteMarginDto?> GetByIdAsync(string instId)
        {
            var entity = await _context.RoutesMargin.AsNoTracking().FirstOrDefaultAsync(r => r.inst_id == instId);
            return entity == null ? null : await BuildDtoAsync(entity);
        }

        public async Task<RouteMarginDto> CreateAsync(CreateRouteMarginRequest request, string actingUser)
        {
            await RouteCategoryRules.EnsureTopupAsync(_context, request.InstId);

            if (await _context.RoutesMargin.AsNoTracking().AnyAsync(r => r.inst_id == request.InstId))
                throw new ConflictException($"Product '{request.InstId}' already has a margin route.");

            var entity = new SwRoutesMargin
            {
                inst_id = request.InstId,
                notes = request.Notes
            };

            _context.RoutesMargin.Add(entity);
            _audit.LogInsert(entity, "sw_routes_margin", actingUser);

            await _context.SaveChangesAsync();
            return await BuildDtoAsync(entity);
        }

        public async Task<RouteMarginDto> UpdateAsync(string instId, UpdateRouteMarginRequest request, string actingUser)
        {
            var entity = await _context.RoutesMargin.FirstOrDefaultAsync(r => r.inst_id == instId)
                ?? throw new NotFoundException($"Route margin '{instId}' not found.");

            var before = await BuildDtoAsync(entity);
            entity.notes = request.Notes;

            _audit.LogUpdate(before, await BuildDtoAsync(entity), "sw_routes_margin", actingUser);

            await _context.SaveChangesAsync();
            return await BuildDtoAsync(entity);
        }

        public async Task DeleteAsync(string instId, string actingUser)
        {
            var entity = await _context.RoutesMargin.FirstOrDefaultAsync(r => r.inst_id == instId)
                ?? throw new NotFoundException($"Route margin '{instId}' not found.");

            _context.RoutesMargin.Remove(entity);
            _audit.LogDelete(entity, "sw_routes_margin", actingUser);

            await _context.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<RouteMarginDto>> SyncCategoryAsync(SyncRouteMarginRequest request, string actingUser)
        {
            // only product codes that actually belong to Category are honored, so a stale
            // client payload can't route a product into the wrong category's set.
            var categoryProductCodes = await _context.Products.AsNoTracking()
                .Where(p => p.category == request.Category)
                .Select(p => p.product_code)
                .ToListAsync();

            // Routing > Margin hanya untuk kategori topup
            var category = await _context.ProductCategories.AsNoTracking()
                .FirstOrDefaultAsync(c => c.category == request.Category);
            if (category?.is_topup != "1")
                throw new ValidationException($"Kategori '{request.Category}' bukan kategori topup. Routing > Margin hanya untuk produk topup.");

            var desired = request.ProductCodes.Intersect(categoryProductCodes).ToHashSet();

            var current = await _context.RoutesMargin
                .Where(r => categoryProductCodes.Contains(r.inst_id))
                .ToListAsync();

            var toRemove = current.Where(r => !desired.Contains(r.inst_id)).ToList();
            var toAdd = desired.Where(code => !current.Any(r => r.inst_id == code)).ToList();

            foreach (var entity in toRemove)
            {
                _context.RoutesMargin.Remove(entity);
                _audit.LogDelete(entity, "sw_routes_margin", actingUser);
            }

            // baris baru bermode STATIC (default entity); mode diatur di Product > Prices > Supplier Prices
            foreach (var code in toAdd)
            {
                var entity = new SwRoutesMargin { inst_id = code };
                _context.RoutesMargin.Add(entity);
                _audit.LogInsert(entity, "sw_routes_margin", actingUser);
            }

            await _context.SaveChangesAsync();

            var result = await _context.RoutesMargin.AsNoTracking()
                .Where(r => categoryProductCodes.Contains(r.inst_id))
                .OrderBy(r => r.inst_id)
                .ToListAsync();

            var ids = result.Select(r => r.inst_id).ToList();
            var supplierCounts = await ActiveSupplierCountsAsync(ids);
            var sourceRules = await SourceRuleProductsAsync(ids);

            return result.Select(e => ToDto(e, supplierCounts, sourceRules)).ToList();
        }

        private async Task<RouteMarginDto> BuildDtoAsync(SwRoutesMargin entity)
        {
            var supplierCounts = await ActiveSupplierCountsAsync([entity.inst_id]);
            var sourceRules = await SourceRuleProductsAsync([entity.inst_id]);

            return ToDto(entity, supplierCounts, sourceRules);
        }

        private async Task<Dictionary<string, int>> ActiveSupplierCountsAsync(IReadOnlyCollection<string> instIds)
        {
            var rows = await _context.MarginSuppliers.AsNoTracking()
                .Where(s => s.status == "1" && s.biller_code != null && instIds.Contains(s.biller_code))
                .Select(s => new { s.biller_code, s.supplier_id })
                .ToListAsync();

            return rows
                .GroupBy(r => r.biller_code!)
                .ToDictionary(g => g.Key, g => g.Select(r => r.supplier_id).Distinct().Count());
        }

        private async Task<HashSet<string>> SourceRuleProductsAsync(IReadOnlyCollection<string> instIds)
        {
            var ids = await _context.RoutesBySource.AsNoTracking()
                .Where(r => r.inst_id != null && instIds.Contains(r.inst_id))
                .Select(r => r.inst_id!)
                .Distinct()
                .ToListAsync();

            return ids.ToHashSet();
        }

        private static RouteMarginDto ToDto(SwRoutesMargin e, Dictionary<string, int> supplierCounts, HashSet<string> sourceRules)
            => new(e.inst_id, e.notes, e.routing_mode,
                supplierCounts.GetValueOrDefault(e.inst_id), sourceRules.Contains(e.inst_id));
    }
}
