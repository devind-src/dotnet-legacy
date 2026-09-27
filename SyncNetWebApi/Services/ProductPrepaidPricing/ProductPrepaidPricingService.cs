using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.ProductPrepaidPricing;
using SyncNetApi.Dtos.Routing;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;
using SyncNetApi.Services.RouteRules;

namespace SyncNetApi.Services.ProductPrepaidPricing
{
    public class ProductPrepaidPricingService : IProductPrepaidPricingService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public ProductPrepaidPricingService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<ProductPrepaidPricingDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.MarginSuppliers.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(x => (x.supplier_id ?? "").ToLower().Contains(f)
                    || (x.biller_code ?? "").ToLower().Contains(f)
                    || (x.product_name ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(x => x.id).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<ProductPrepaidPricingDto?> GetByIdAsync(int id)
        {
            var entity = await _context.MarginSuppliers.AsNoTracking().FirstOrDefaultAsync(x => x.id == id);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<ProductPrepaidPricingDto> CreateAsync(CreateProductPrepaidPricingRequest request, string actingUser)
        {
            if (!await _context.Products.AsNoTracking().AnyAsync(p => p.product_code == request.BillerCode))
                throw new ValidationException($"Product '{request.BillerCode}' does not exist.");

            await EnsureNotDuplicateAsync(request.SupplierId, request.BillerCode, request.Denom, ignoreId: null);

            var margin = (request.HargaJual ?? 0) - request.HargaBeli;
            if (margin < 0)
                throw new ValidationException("Margin lebih kecil dari NOL, silahkan cek harga jual.");

            // product_name is denormalized from Product Master at save time (legacy
            // ProductMasterGetProductName), not kept in sync afterwards.
            var productName = await _context.Products.AsNoTracking()
                .Where(p => p.product_code == request.BillerCode).Select(p => p.product_name).FirstOrDefaultAsync();

            int priority = request.Priority ?? await NextPriorityAsync(request.BillerCode, request.Denom);
            await EnsurePriorityFreeAsync(request.BillerCode, request.Denom, priority, ignoreId: null);

            var entity = new SwMarginSupplier
            {
                supplier_id = request.SupplierId,
                biller_code = request.BillerCode,
                product_name = productName,
                denom = request.Denom,
                harga_beli = request.HargaBeli,
                harga_jual = request.HargaJual,
                margin = margin,
                status = request.Active ? "1" : "0",
                priority = (short)priority,
                lb_weight = request.LbWeight.HasValue ? (short)request.LbWeight.Value : null
            };

            if ((request.LbWeight ?? 0) > 0)
                await EnsureLbTotalWhenLoadBalanceAsync(entity, isNew: true);

            _context.MarginSuppliers.Add(entity);
            _audit.LogInsert(entity, "sw_margin_supplier", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<ProductPrepaidPricingDto> UpdateAsync(int id, UpdateProductPrepaidPricingRequest request, string actingUser)
        {
            var entity = await _context.MarginSuppliers.FirstOrDefaultAsync(x => x.id == id)
                ?? throw new NotFoundException($"Supplier price '{id}' not found.");

            await EnsureNotDuplicateAsync(entity.supplier_id, entity.biller_code, request.Denom, ignoreId: id);

            var margin = (request.HargaJual ?? 0) - request.HargaBeli;
            if (margin < 0)
                throw new ValidationException("Margin lebih kecil dari NOL, silahkan cek harga jual.");

            // product_name re-resolved from Product Master, same as legacy Save().
            var productName = await _context.Products.AsNoTracking()
                .Where(p => p.product_code == entity.biller_code).Select(p => p.product_name).FirstOrDefaultAsync();

            int priority = request.Priority ?? entity.priority ?? await NextPriorityAsync(entity.biller_code, request.Denom);
            await EnsurePriorityFreeAsync(entity.biller_code, request.Denom, priority, ignoreId: id);

            var before = ToDto(entity);
            bool weightChanged = entity.lb_weight != (request.LbWeight.HasValue ? (short)request.LbWeight.Value : null);
            entity.product_name = productName;
            entity.denom = request.Denom;
            entity.harga_beli = request.HargaBeli;
            entity.harga_jual = request.HargaJual;
            entity.margin = margin;
            entity.status = request.Active ? "1" : "0";
            entity.priority = (short)priority;
            entity.lb_weight = request.LbWeight.HasValue ? (short)request.LbWeight.Value : null;

            if (weightChanged)
                await EnsureLbTotalWhenLoadBalanceAsync(entity, isNew: false);

            _audit.LogUpdate(before, ToDto(entity), "sw_margin_supplier", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(int id, string actingUser)
        {
            var entity = await _context.MarginSuppliers.FirstOrDefaultAsync(x => x.id == id)
                ?? throw new NotFoundException($"Supplier price '{id}' not found.");

            _context.MarginSuppliers.Remove(entity);
            _audit.LogDelete(entity, "sw_margin_supplier", actingUser);

            await _context.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<TopupRoutingDto>> GetTopupRoutingAsync(string? filter = null)
        {
            var priced = await _context.MarginSuppliers.AsNoTracking()
                .Where(s => s.biller_code != null)
                .Select(s => s.biller_code!)
                .Distinct()
                .ToListAsync();
            var routed = await _context.RoutesMargin.AsNoTracking().ToListAsync();

            var ids = priced.Union(routed.Select(r => r.inst_id)).Distinct().ToList();

            var products = await (from p in _context.Products.AsNoTracking()
                                  join c in _context.ProductCategories.AsNoTracking() on p.category equals c.category
                                  where c.is_topup == "1" && ids.Contains(p.product_code)
                                  select new { p.product_code, p.product_name })
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                products = products.Where(p => p.product_code.ToLower().Contains(f) || (p.product_name ?? "").ToLower().Contains(f)).ToList();
            }

            var productIds = products.Select(p => p.product_code).ToList();
            var counts = await ActiveSupplierCountsAsync(productIds);
            var sourceRules = (await _context.RoutesBySource.AsNoTracking()
                .Where(r => r.inst_id != null && productIds.Contains(r.inst_id))
                .Select(r => r.inst_id!).Distinct().ToListAsync()).ToHashSet();
            var nodeIds = routed.Where(r => r.static_node_id.HasValue).Select(r => r.static_node_id!.Value).ToList();
            var nodeNames = await _context.Nodes.AsNoTracking().Where(n => nodeIds.Contains(n.node_id))
                .ToDictionaryAsync(n => n.node_id, n => n.node_name);

            return products
                .OrderBy(p => p.product_code)
                .Select(p =>
                {
                    var route = routed.FirstOrDefault(r => r.inst_id == p.product_code);
                    return new TopupRoutingDto(p.product_code, p.product_name, route != null,
                        route?.routing_mode ?? RoutingModes.Static, route?.static_node_id,
                        route?.static_node_id is int nid ? nodeNames.GetValueOrDefault(nid) : null,
                        counts.GetValueOrDefault(p.product_code), sourceRules.Contains(p.product_code));
                })
                .ToList();
        }

        public async Task<TopupRoutingDto> UpdateTopupRoutingAsync(string productId, UpdateTopupRoutingRequest request, string actingUser)
        {
            await RouteCategoryRules.EnsureTopupAsync(_context, productId);

            string mode = request.RoutingMode;
            if (!RoutingModes.IsValid(mode))
                throw new ValidationException($"Routing mode '{mode}' tidak dikenal.");

            var suppliers = await _context.MarginSuppliers.AsNoTracking()
                .Where(s => s.biller_code == productId)
                .ToListAsync();
            var active = suppliers.Where(s => s.status == "1").ToList();

            int? staticNodeId = null;
            if (mode == RoutingModes.Static && request.StaticNodeId.HasValue)
            {
                var node = await _context.Nodes.AsNoTracking().FirstOrDefaultAsync(n => n.node_id == request.StaticNodeId.Value)
                    ?? throw new ValidationException($"Node '{request.StaticNodeId}' does not exist.");
                if (suppliers.All(s => s.supplier_id != node.node_name))
                    throw new ValidationException($"Supplier '{node.node_name}' tidak punya harga untuk produk '{productId}'.");
                staticNodeId = node.node_id;
            }

            if (RoutingModes.IsDynamic(mode))
            {
                int distinct = active.Select(s => s.supplier_id).Distinct().Count();
                if (distinct < 2)
                    throw new ValidationException(
                        $"Mode {RoutingModes.Label(mode)} butuh minimal 2 supplier aktif untuk produk '{productId}' (sekarang {distinct}).");

                await RouteCategoryRules.EnsureNoSourceRuleAsync(_context, productId);
            }

            if (mode == RoutingModes.LoadBalance)
            {
                foreach (var denom in active.GroupBy(s => s.denom))
                {
                    int weight = denom.Sum(s => (int)(s.lb_weight ?? 0));
                    if (weight != 100)
                        throw new ValidationException(
                            $"Mode Load Balance butuh total LB Weight supplier aktif tepat 100% per denom (denom {denom.Key:N0}: {weight}%).");
                }
            }

            var entity = await _context.RoutesMargin.FirstOrDefaultAsync(r => r.inst_id == productId);
            if (entity == null)
            {
                entity = new SwRoutesMargin { inst_id = productId, routing_mode = mode, static_node_id = staticNodeId };
                _context.RoutesMargin.Add(entity);
                _audit.LogInsert(entity, "sw_routes_margin", actingUser);
            }
            else
            {
                var before = new { entity.inst_id, entity.routing_mode, entity.static_node_id };
                entity.routing_mode = mode;
                entity.static_node_id = staticNodeId;
                _audit.LogUpdate(before, new { entity.inst_id, entity.routing_mode, entity.static_node_id }, "sw_routes_margin", actingUser);
            }

            await _context.SaveChangesAsync();

            return (await GetTopupRoutingAsync()).First(r => r.ProductId == productId);
        }

        public async Task<IReadOnlyList<ProductPrepaidPricingDto>> ApplyToAllDenomsAsync(int id, string actingUser)
        {
            var source = await _context.MarginSuppliers.AsNoTracking().FirstOrDefaultAsync(x => x.id == id)
                ?? throw new NotFoundException($"Supplier price '{id}' not found.");

            var rows = await _context.MarginSuppliers
                .Where(s => s.biller_code == source.biller_code && s.supplier_id == source.supplier_id && s.id != id)
                .ToListAsync();

            foreach (var row in rows)
            {
                // priority harus tetap unik per denom
                if (source.priority.HasValue && await _context.MarginSuppliers.AsNoTracking().AnyAsync(s =>
                        s.biller_code == row.biller_code && s.denom == row.denom && s.id != row.id && s.priority == source.priority))
                    throw new ConflictException(
                        $"Priority {source.priority} sudah dipakai supplier lain pada denom {row.denom:N0}. Ubah priority denom itu dulu.");

                var before = ToDto(row);
                row.priority = source.priority;
                row.lb_weight = source.lb_weight;
                _audit.LogUpdate(before, ToDto(row), "sw_margin_supplier", actingUser);
            }

            // pada mode Load Balance, bobot yang disalin tidak boleh membuat total denom lain lepas dari 100
            if (rows.Any(r => r.lb_weight != null) && await _context.RoutesMargin.AsNoTracking()
                    .AnyAsync(r => r.inst_id == source.biller_code && r.routing_mode == RoutingModes.LoadBalance))
            {
                foreach (var row in rows)
                {
                    int others = await _context.MarginSuppliers.AsNoTracking()
                        .Where(s => s.biller_code == row.biller_code && s.denom == row.denom && s.id != row.id && s.status == "1")
                        .SumAsync(s => (int?)s.lb_weight) ?? 0;
                    int total = others + (row.status == "1" ? row.lb_weight ?? 0 : 0);
                    if (total != 100)
                        throw new ValidationException(
                            $"Produk '{row.biller_code}' memakai Load Balance: menyalin LB Weight {source.lb_weight}% membuat total denom {row.denom:N0} menjadi {total}%. Atur bobot lewat tombol Atur LB Weight pada denom itu.");
                }
            }

            await _context.SaveChangesAsync();

            var all = await _context.MarginSuppliers.AsNoTracking()
                .Where(s => s.biller_code == source.biller_code && s.supplier_id == source.supplier_id)
                .ToListAsync();
            return all.Select(ToDto).ToList();
        }

        public async Task<IReadOnlyList<ProductPrepaidPricingDto>> UpdateWeightsAsync(string productId, UpdateSupplierWeightsRequest request, string actingUser)
        {
            var rows = await _context.MarginSuppliers
                .Where(s => s.biller_code == productId && s.denom == request.Denom)
                .ToListAsync();

            foreach (var w in request.Weights)
            {
                if (w.LbWeight < 0 || w.LbWeight > 100)
                    throw new ValidationException("LB Weight harus 0 sampai 100.");

                var row = rows.FirstOrDefault(r => r.id == w.Id)
                    ?? throw new ValidationException($"Supplier price '{w.Id}' bukan milik produk '{productId}' denom {request.Denom:N0}.");

                var before = ToDto(row);
                row.lb_weight = (short)w.LbWeight;
                _audit.LogUpdate(before, ToDto(row), "sw_margin_supplier", actingUser);
            }

            int total = rows.Where(r => r.status == "1").Sum(r => (int)(r.lb_weight ?? 0));
            if (total != 100)
                throw new ValidationException(
                    $"Total LB Weight supplier aktif denom {request.Denom:N0} harus tepat 100% (sekarang {total}%).");

            await _context.SaveChangesAsync();
            return rows.Select(ToDto).ToList();
        }

        // pada mode Load Balance, bobot yang diubah lewat satu baris harus menjaga total supplier aktif
        // denom itu tepat 100; mengubah bobot beberapa supplier sekaligus lewat Atur LB Weight
        private async Task EnsureLbTotalWhenLoadBalanceAsync(SwMarginSupplier entity, bool isNew)
        {
            bool loadBalance = await _context.RoutesMargin.AsNoTracking()
                .AnyAsync(r => r.inst_id == entity.biller_code && r.routing_mode == RoutingModes.LoadBalance);
            if (!loadBalance) return;

            var others = await _context.MarginSuppliers.AsNoTracking()
                .Where(s => s.biller_code == entity.biller_code && s.denom == entity.denom && s.id != entity.id && s.status == "1")
                .SumAsync(s => (int?)s.lb_weight) ?? 0;
            int total = others + (entity.status == "1" ? entity.lb_weight ?? 0 : 0);

            if (total != 100)
                throw new ValidationException(
                    $"Produk '{entity.biller_code}' memakai Load Balance: total LB Weight supplier aktif denom {entity.denom:N0} harus tetap 100% (menjadi {total}%). Gunakan tombol Atur LB Weight pada denom itu untuk mengubah bobot semua supplier sekaligus.");
        }

        // unique index IX_sw_margin_supplier_denom (supplier_id, biller_code, denom): diperiksa lebih
        // dulu supaya duplikat dijawab 409 dengan pesan jelas, bukan error database 500
        private async Task EnsureNotDuplicateAsync(string? supplierId, string? productId, int? denom, int? ignoreId)
        {
            if (await _context.MarginSuppliers.AsNoTracking().AnyAsync(s =>
                    s.supplier_id == supplierId && s.biller_code == productId && s.denom == denom && s.id != (ignoreId ?? -1)))
                throw new ConflictException(
                    $"Data duplikat: supplier '{supplierId}' sudah punya harga untuk produk '{productId}' denom {denom:N0}. Ubah baris yang sudah ada.");
        }

        private async Task<int> NextPriorityAsync(string? productId, int? denom)
            => (await _context.MarginSuppliers.AsNoTracking()
                .Where(s => s.biller_code == productId && s.denom == denom)
                .Select(s => (int?)s.priority)
                .MaxAsync() ?? 0) + 1;

        private async Task EnsurePriorityFreeAsync(string? productId, int? denom, int priority, int? ignoreId)
        {
            if (await _context.MarginSuppliers.AsNoTracking().AnyAsync(s =>
                    s.biller_code == productId && s.denom == denom && s.priority == priority && s.id != (ignoreId ?? -1)))
                throw new ConflictException($"Priority {priority} sudah dipakai supplier lain untuk produk dan denom ini.");
        }

        private async Task<Dictionary<string, int>> ActiveSupplierCountsAsync(IReadOnlyCollection<string> productIds)
        {
            var rows = await _context.MarginSuppliers.AsNoTracking()
                .Where(s => s.status == "1" && s.biller_code != null && productIds.Contains(s.biller_code))
                .Select(s => new { s.biller_code, s.supplier_id })
                .ToListAsync();

            return rows.GroupBy(r => r.biller_code!).ToDictionary(g => g.Key, g => g.Select(r => r.supplier_id).Distinct().Count());
        }

        private static ProductPrepaidPricingDto ToDto(SwMarginSupplier e) => new(e.id, e.supplier_id, e.biller_code, e.product_name,
            e.denom, e.harga_beli, e.harga_jual, e.margin, e.status == "1", e.priority, e.lb_weight);
    }
}
