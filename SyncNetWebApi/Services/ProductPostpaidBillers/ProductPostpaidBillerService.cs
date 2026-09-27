using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.ProductPostpaidBillers;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;
using SyncNetApi.Services.RouteRules;

namespace SyncNetApi.Services.ProductPostpaidBillers
{
    public interface IProductPostpaidBillerService
    {
        Task<ProductPostpaidBillersDto> GetByProductAsync(string productId);
        Task<ProductPostpaidBillersDto> CreateAsync(CreateProductPostpaidBillerRequest request, string actingUser);
        Task<ProductPostpaidBillersDto> UpdateAsync(string productId, int nodeId, UpdateProductPostpaidBillerRequest request, string actingUser);
        Task<ProductPostpaidBillersDto> DeleteAsync(string productId, int nodeId, string actingUser);
        Task<ProductPostpaidBillersDto> UpdateWeightsAsync(string productId, UpdateProductPostpaidBillerWeightsRequest request, string actingUser);
    }

    /// <summary>Detail biller Product &gt; Fees &gt; Product Fees (fase 1). Satu daftar untuk
    /// primary (sw_routes_by_inst, priority 1, selalu aktif) dan alternate (sw_routes_by_inst_alt).
    /// Biller pertama menjadi primary; priority alternate diubah ke 1 = tukar dengan primary.
    /// Setiap perubahan diperiksa terhadap baris fee produk (ProductRoutingRules).</summary>
    public class ProductPostpaidBillerService : IProductPostpaidBillerService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public ProductPostpaidBillerService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<ProductPostpaidBillersDto> GetByProductAsync(string productId)
        {
            var productName = await _context.Products.AsNoTracking()
                .Where(p => p.product_code == productId).Select(p => p.product_name).FirstOrDefaultAsync();

            var billers = await ProductRoutingRules.LoadBillersAsync(_context, productId);
            var nodeIds = billers.Select(b => b.NodeId).ToList();
            var nodes = await _context.Nodes.AsNoTracking().Where(n => nodeIds.Contains(n.node_id)).ToListAsync();

            var appNames = nodes.Select(n => n.app_name).Where(a => a != null).Distinct().ToList();
            var mappings = await _context.ProductMappings.AsNoTracking()
                .Where(m => m.source_biller_code == productId && appNames.Contains(m.app_name))
                .ToListAsync();

            var nodeNames = billers.Select(b => b.NodeName).ToList();
            var health = await _context.RoutesSupplierStatus.AsNoTracking()
                .Where(s => nodeNames.Contains(s.supplier_id))
                .ToDictionaryAsync(s => s.supplier_id, s => s.status);

            var notes = await NotesAsync(productId);

            var items = billers.Select(b =>
            {
                var node = nodes.FirstOrDefault(n => n.node_id == b.NodeId);
                var code = mappings.FirstOrDefault(m => m.app_name == node?.app_name)?.dest_biller_code;
                return new ProductPostpaidBillerDto(productId, b.NodeId, b.NodeName, node?.app_name, code,
                    b.Priority, b.IsPrimary, b.FeeSharing, b.LbWeight, notes.GetValueOrDefault(b.NodeId), b.Active,
                    health.GetValueOrDefault(b.NodeName, "ACTIVE"));
            }).ToList();

            return new ProductPostpaidBillersDto(productId, productName,
                await RouteCategoryRules.HasSourceRuleAsync(_context, productId), items);
        }

        public async Task<ProductPostpaidBillersDto> CreateAsync(CreateProductPostpaidBillerRequest request, string actingUser)
        {
            string productId = request.ProductId;
            await EnsureBillPaymentOrPurchaseAsync(productId);

            if (!await _context.Nodes.AsNoTracking().AnyAsync(n => n.node_id == request.NodeId))
                throw new ValidationException($"Node '{request.NodeId}' does not exist.");

            var primary = await _context.RoutesByInst.FirstOrDefaultAsync(r => r.inst_id == productId);
            var alternates = await _context.RoutesByInstAlt.Where(a => a.inst_id == productId).ToListAsync();

            if (primary == null)
            {
                //biller pertama otomatis menjadi primary (F3)
                if (!request.Active)
                    throw new ValidationException("Biller pertama menjadi primary dan harus aktif.");

                primary = new SwRoutesByInst
                {
                    inst_id = productId,
                    node_id = request.NodeId,
                    notes = request.Notes ?? string.Empty,
                    fee_sharing = request.FeeSharing,
                    lb_weight = ToShort(request.LbWeight)
                };

                await ValidateAsync(productId, primary, alternates, request.NodeId, checkLbTotal: false);

                _context.RoutesByInst.Add(primary);
                _audit.LogInsert(primary, "sw_routes_by_inst", actingUser);
                await _context.SaveChangesAsync();

                return await GetByProductAsync(productId);
            }

            if (primary.node_id == request.NodeId || alternates.Any(a => a.node_id == request.NodeId))
                throw new ConflictException("Biller itu sudah terdaftar untuk produk ini.");

            int priority = request.Priority ?? NextPriority(alternates);
            var now = LocalClock.Now;

            var alt = new SwRoutesByInstAlt
            {
                inst_id = productId,
                node_id = request.NodeId,
                fee_sharing = request.FeeSharing,
                lb_weight = ToShort(request.LbWeight),
                notes = request.Notes,
                status = request.Active ? "1" : "0",
                created_by = actingUser,
                created_dt = now,
                updated_by = actingUser,
                updated_dt = now
            };

            if (priority == 1)
            {
                //biller baru menjadi primary, primary lama pindah ke urutan berikutnya
                if (!request.Active)
                    throw new ValidationException("Biller dengan priority 1 menjadi primary dan harus aktif.");

                alt.priority = (short)NextPriority(alternates);
                SwapWithPrimary(primary, alt);
            }
            else
            {
                EnsurePriorityFree(alternates, priority, ignore: null);
                alt.priority = (short)priority;
            }

            alternates.Add(alt);
            await ValidateAsync(productId, primary, alternates, request.NodeId, checkLbTotal: (request.LbWeight ?? 0) > 0);

            _context.RoutesByInstAlt.Add(alt);
            _audit.LogInsert(alt, "sw_routes_by_inst_alt", actingUser);
            await _context.SaveChangesAsync();

            return await GetByProductAsync(productId);
        }

        public async Task<ProductPostpaidBillersDto> UpdateAsync(string productId, int nodeId, UpdateProductPostpaidBillerRequest request, string actingUser)
        {
            var primary = await _context.RoutesByInst.FirstOrDefaultAsync(r => r.inst_id == productId)
                ?? throw new NotFoundException($"Produk '{productId}' belum punya biller.");
            var alternates = await _context.RoutesByInstAlt.Where(a => a.inst_id == productId).ToListAsync();

            if (primary.node_id == nodeId)
            {
                if (request.Priority != 1)
                    throw new ValidationException("Untuk mengganti primary, ubah priority biller lain menjadi 1.");
                if (!request.Active)
                    throw new ValidationException("Primary tidak bisa dinonaktifkan. Pindahkan priority 1 ke biller lain lebih dulu.");

                var before = new { primary.inst_id, primary.node_id, primary.fee_sharing, primary.lb_weight, primary.notes };
                bool primaryWeightChanged = primary.lb_weight != ToShort(request.LbWeight);
                primary.fee_sharing = request.FeeSharing;
                primary.lb_weight = ToShort(request.LbWeight);
                primary.notes = request.Notes ?? primary.notes;

                await ValidateAsync(productId, primary, alternates, nodeId, checkLbTotal: primaryWeightChanged);

                _audit.LogUpdate(before, new { primary.inst_id, primary.node_id, primary.fee_sharing, primary.lb_weight, primary.notes },
                    "sw_routes_by_inst", actingUser);
                await _context.SaveChangesAsync();
                return await GetByProductAsync(productId);
            }

            var alt = alternates.FirstOrDefault(a => a.node_id == nodeId)
                ?? throw new NotFoundException($"Biller '{nodeId}' tidak terdaftar untuk produk '{productId}'.");

            var altBefore = new { alt.id, alt.node_id, alt.priority, alt.fee_sharing, alt.lb_weight, alt.notes, alt.status };
            bool weightChanged = alt.lb_weight != ToShort(request.LbWeight);

            alt.fee_sharing = request.FeeSharing;
            alt.lb_weight = ToShort(request.LbWeight);
            alt.notes = request.Notes;
            alt.status = request.Active ? "1" : "0";
            alt.updated_by = actingUser;
            alt.updated_dt = LocalClock.Now;

            if (request.Priority == 1)
            {
                //alternate naik menjadi primary; primary lama mengambil slot priority alternate ini (F3)
                if (!request.Active)
                    throw new ValidationException("Biller dengan priority 1 menjadi primary dan harus aktif.");

                SwapWithPrimary(primary, alt);
            }
            else if (request.Priority != alt.priority)
            {
                EnsurePriorityFree(alternates, request.Priority, ignore: alt);
                alt.priority = (short)request.Priority;
            }

            await ValidateAsync(productId, primary, alternates, nodeId, checkLbTotal: weightChanged);

            _audit.LogUpdate(altBefore, new { alt.id, alt.node_id, alt.priority, alt.fee_sharing, alt.lb_weight, alt.notes, alt.status },
                "sw_routes_by_inst_alt", actingUser);
            await _context.SaveChangesAsync();

            return await GetByProductAsync(productId);
        }

        public async Task<ProductPostpaidBillersDto> DeleteAsync(string productId, int nodeId, string actingUser)
        {
            var primary = await _context.RoutesByInst.FirstOrDefaultAsync(r => r.inst_id == productId)
                ?? throw new NotFoundException($"Produk '{productId}' belum punya biller.");
            var alternates = await _context.RoutesByInstAlt.Where(a => a.inst_id == productId).ToListAsync();

            if (primary.node_id == nodeId)
            {
                if (alternates.Count > 0)
                    throw new ValidationException(
                        "Primary tidak bisa dihapus selama masih ada biller lain. Pindahkan priority 1 ke biller lain lebih dulu.");

                await ValidateAsync(productId, null, alternates, editedNodeId: null, checkLbTotal: false);

                _context.RoutesByInst.Remove(primary);
                _audit.LogDelete(primary, "sw_routes_by_inst", actingUser);
                await _context.SaveChangesAsync();
                return await GetByProductAsync(productId);
            }

            var alt = alternates.FirstOrDefault(a => a.node_id == nodeId)
                ?? throw new NotFoundException($"Biller '{nodeId}' tidak terdaftar untuk produk '{productId}'.");

            alternates.Remove(alt);
            await ValidateAsync(productId, primary, alternates, editedNodeId: null, checkLbTotal: false);

            _context.RoutesByInstAlt.Remove(alt);
            _audit.LogDelete(alt, "sw_routes_by_inst_alt", actingUser);
            await _context.SaveChangesAsync();

            return await GetByProductAsync(productId);
        }

        public async Task<ProductPostpaidBillersDto> UpdateWeightsAsync(string productId, UpdateProductPostpaidBillerWeightsRequest request, string actingUser)
        {
            var primary = await _context.RoutesByInst.FirstOrDefaultAsync(r => r.inst_id == productId)
                ?? throw new NotFoundException($"Produk '{productId}' belum punya biller.");
            var alternates = await _context.RoutesByInstAlt.Where(a => a.inst_id == productId).ToListAsync();

            foreach (var w in request.Weights)
            {
                if (w.LbWeight < 0 || w.LbWeight > 100)
                    throw new ValidationException("LB Weight harus 0 sampai 100.");

                if (primary.node_id == w.NodeId)
                    primary.lb_weight = (short)w.LbWeight;
                else
                {
                    var alt = alternates.FirstOrDefault(a => a.node_id == w.NodeId)
                        ?? throw new ValidationException($"Biller '{w.NodeId}' tidak terdaftar untuk produk '{productId}'.");
                    alt.lb_weight = (short)w.LbWeight;
                    alt.updated_by = actingUser;
                    alt.updated_dt = LocalClock.Now;
                }
            }

            var billers = await ProductRoutingRules.BuildBillersAsync(_context, primary, alternates);
            int total = billers.Where(b => b.Active).Sum(b => b.LbWeight ?? 0);
            if (total != 100)
                throw new ValidationException($"Total LB Weight biller aktif harus tepat 100% (sekarang {total}%).");

            await ValidateAsync(productId, primary, alternates, editedNodeId: null, checkLbTotal: true);

            _audit.LogUpdate(new { productId }, new { productId, request.Weights }, "sw_routes_by_inst_alt", actingUser);
            await _context.SaveChangesAsync();

            return await GetByProductAsync(productId);
        }

        private async Task ValidateAsync(string productId, SwRoutesByInst? primary, IEnumerable<SwRoutesByInstAlt> alternates,
            int? editedNodeId, bool checkLbTotal)
        {
            var billers = await ProductRoutingRules.BuildBillersAsync(_context, primary, alternates);
            var rows = await _context.Fees.AsNoTracking().Where(f => f.product_id == productId).ToListAsync();

            await ProductRoutingRules.EnsureConsistentAsync(_context, productId, rows, billers, checkLbTotal, editedNodeId);
        }

        private async Task EnsureBillPaymentOrPurchaseAsync(string productId)
        {
            var topup = await RouteCategoryRules.IsTopupAsync(_context, productId)
                ?? throw new ValidationException($"Produk '{productId}' tidak ditemukan.");

            if (topup)
                throw new ValidationException(
                    $"Produk '{productId}' kategori topup. Biller topup diatur lewat Product > Prices > Supplier Prices.");
        }

        // node, sharing fee, bobot, dan catatan milik biller ikut berpindah; slot priority tetap
        private static void SwapWithPrimary(SwRoutesByInst primary, SwRoutesByInstAlt alt)
        {
            (primary.node_id, alt.node_id) = (alt.node_id, primary.node_id);
            (primary.fee_sharing, alt.fee_sharing) = (alt.fee_sharing, primary.fee_sharing);
            (primary.lb_weight, alt.lb_weight) = (alt.lb_weight, primary.lb_weight);
            alt.status = "1";
        }

        private static void EnsurePriorityFree(IEnumerable<SwRoutesByInstAlt> alternates, int priority, SwRoutesByInstAlt? ignore)
        {
            if (priority < 1)
                throw new ValidationException("Priority minimal 1.");

            if (alternates.Any(a => a != ignore && a.priority == priority))
                throw new ConflictException($"Priority {priority} sudah dipakai biller lain untuk produk ini.");
        }

        private static int NextPriority(IEnumerable<SwRoutesByInstAlt> alternates)
            => alternates.Select(a => (int)a.priority).DefaultIfEmpty(1).Max() + 1;

        private static short? ToShort(int? value) => value.HasValue ? (short)value.Value : null;

        // catatan biller: primary memakai notes route, alternate memakai notes alternate
        private async Task<Dictionary<int, string?>> NotesAsync(string productId)
        {
            var result = new Dictionary<int, string?>();

            var primary = await _context.RoutesByInst.AsNoTracking().FirstOrDefaultAsync(r => r.inst_id == productId);
            if (primary != null) result[primary.node_id] = primary.notes;

            foreach (var alt in await _context.RoutesByInstAlt.AsNoTracking().Where(a => a.inst_id == productId).ToListAsync())
                result[alt.node_id] = alt.notes;

            return result;
        }
    }
}
