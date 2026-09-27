using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.ProductPostpaidFees;
using SyncNetApi.Dtos.Routing;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;
using SyncNetApi.Services.RouteRules;

namespace SyncNetApi.Services.ProductPostpaidFees
{
    /// <summary>Product &gt; Fees &gt; Product Fees: fee Bill Payment &amp; Purchase per produk
    /// (baris default) dan per CA (merchant), sekaligus Routing Mode (fase 1 K1-K22). Detail
    /// biller produk dikelola ProductPostpaidBillerService; aturan konsistensi di ProductRoutingRules.</summary>
    public class ProductPostpaidFeeService : IProductPostpaidFeeService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public ProductPostpaidFeeService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        // INNER JOIN Product Master (baris fee yang produknya sudah tidak ada ikut hilang, sama
        // dengan legacy), LEFT JOIN Merchant/SubMerchant. Produk kategori topup tidak ditampilkan:
        // fee topup diatur lewat Supplier Prices/Merchant Prices.
        public async Task<IReadOnlyList<ProductPostpaidFeeDto>> GetRecordsAsync(string? filter = null)
        {
            var query = from f in _context.Fees.AsNoTracking()
                        join p in _context.Products.AsNoTracking() on f.product_id equals p.product_code
                        join c in _context.ProductCategories.AsNoTracking() on p.category equals c.category into gc
                        from c in gc.DefaultIfEmpty()
                        join m in _context.Merchants.AsNoTracking() on f.merchant_id equals m.merchant_id into gm
                        from m in gm.DefaultIfEmpty()
                        join sm in _context.SubMerchants.AsNoTracking() on f.submerchant_id equals sm.submerchant_id into gsm
                        from sm in gsm.DefaultIfEmpty()
                        where c == null || c.is_topup != "1"
                        select new { f, ProductName = p.product_name, MerchantName = m == null ? null : m.name, SubmerchantName = sm == null ? null : sm.name };

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var flt = filter.Sanitize().ToLower();
                query = query.Where(x => (x.f.product_id ?? "").ToLower().Contains(flt)
                    || (x.ProductName ?? "").ToLower().Contains(flt)
                    || (x.f.merchant_id ?? "").ToLower().Contains(flt)
                    || (x.MerchantName ?? "").ToLower().Contains(flt));
            }

            var list = await query.OrderBy(x => x.f.product_id).ThenBy(x => x.f.merchant_id).ThenBy(x => x.f.id).ToListAsync();

            // mode efektif baris CA butuh baris default produknya, termasuk yang tersaring filter
            var productIds = list.Select(x => x.f.product_id).Distinct().ToList();
            var defaults = await _context.Fees.AsNoTracking()
                .Where(f => productIds.Contains(f.product_id)
                    && (f.merchant_id == null || f.merchant_id == "")
                    && (f.submerchant_id == null || f.submerchant_id == ""))
                .ToListAsync();
            var nodeNames = await NodeNamesAsync(list.Select(x => x.f.static_node_id));

            return list.Select(x => ToDto(x.f, x.ProductName, x.MerchantName, x.SubmerchantName,
                defaults.FirstOrDefault(d => d.product_id == x.f.product_id), nodeNames)).ToList();
        }

        public async Task<ProductPostpaidFeeDto?> GetByIdAsync(long id)
        {
            var entity = await _context.Fees.AsNoTracking().FirstOrDefaultAsync(x => x.id == id);
            if (entity == null) return null;

            var productName = await _context.Products.AsNoTracking()
                .Where(p => p.product_code == entity.product_id).Select(p => p.product_name).FirstOrDefaultAsync();
            var merchantName = string.IsNullOrEmpty(entity.merchant_id) ? null : await _context.Merchants.AsNoTracking()
                .Where(m => m.merchant_id == entity.merchant_id).Select(m => m.name).FirstOrDefaultAsync();
            var submerchantName = string.IsNullOrEmpty(entity.submerchant_id) ? null : await _context.SubMerchants.AsNoTracking()
                .Where(sm => sm.submerchant_id == entity.submerchant_id).Select(sm => sm.name).FirstOrDefaultAsync();

            return await BuildDtoAsync(entity, productName, merchantName, submerchantName);
        }

        public async Task<ProductPostpaidFeeDto> CreateAsync(CreateProductPostpaidFeeRequest request, string actingUser)
        {
            if (!await _context.Products.AsNoTracking().AnyAsync(p => p.product_code == request.ProductId))
                throw new ValidationException($"Product '{request.ProductId}' does not exist.");

            if (await RouteCategoryRules.IsTopupAsync(_context, request.ProductId) == true)
                throw new ValidationException(
                    $"Produk '{request.ProductId}' kategori topup. Product Fees hanya untuk Bill Payment & Purchase, gunakan Product > Prices.");

            if (!string.IsNullOrEmpty(request.MerchantId) && !await _context.Merchants.AsNoTracking().AnyAsync(m => m.merchant_id == request.MerchantId))
                throw new ValidationException($"Merchant '{request.MerchantId}' does not exist.");

            string? merchantId = NullIfEmpty(request.MerchantId);
            string? submerchantId = NullIfEmpty(request.SubmerchantId);

            // SDK mengambil baris pertama bila ada duplikat, jadi kombinasi produk + CA + sub CA harus unik
            if (await _context.Fees.AsNoTracking().AnyAsync(f => f.product_id == request.ProductId
                    && (f.merchant_id ?? "") == (merchantId ?? "")
                    && (f.submerchant_id ?? "") == (submerchantId ?? "")))
                throw new ConflictException(merchantId == null
                    ? $"Produk '{request.ProductId}' sudah punya baris fee default."
                    : $"Produk '{request.ProductId}' sudah punya baris fee untuk CA '{merchantId}'.");

            // id is GENERATED BY DEFAULT AS IDENTITY — do not assign manually (see entity note).
            var entity = new SwFees
            {
                product_id = request.ProductId,
                merchant_id = merchantId,
                submerchant_id = submerchantId,
                group_name = request.GroupName,
                subgroup_name = request.SubgroupName
            };
            Apply(entity, request.FeeType, request.RoutingMode, request.StaticNodeId,
                request.FixedFee, request.FixedFeeAcq, request.FixedFeeMer, request.FixedFeeIss, request.FixedFeeBil, request.FixedFeeSwt,
                request.PercentFee, request.PercentFeeAcq, request.PercentFeeMer, request.PercentFeeIss, request.PercentFeeBil, request.PercentFeeSwt);

            var rows = await _context.Fees.AsNoTracking().Where(f => f.product_id == request.ProductId).ToListAsync();
            rows.Add(entity);
            await ValidateAsync(entity, rows);

            _context.Fees.Add(entity);
            await _context.SaveChangesAsync();

            _audit.LogInsert(entity, "sw_fees", actingUser);
            await _context.SaveChangesAsync();

            return await BuildDtoAsync(entity, null, null, null);
        }

        public async Task<ProductPostpaidFeeDto> UpdateAsync(long id, UpdateProductPostpaidFeeRequest request, string actingUser)
        {
            var entity = await _context.Fees.FirstOrDefaultAsync(x => x.id == id)
                ?? throw new NotFoundException($"Product fee '{id}' not found.");

            var before = await BuildDtoAsync(entity, null, null, null);

            Apply(entity, request.FeeType, request.RoutingMode, request.StaticNodeId,
                request.FixedFee, request.FixedFeeAcq, request.FixedFeeMer, request.FixedFeeIss, request.FixedFeeBil, request.FixedFeeSwt,
                request.PercentFee, request.PercentFeeAcq, request.PercentFeeMer, request.PercentFeeIss, request.PercentFeeBil, request.PercentFeeSwt);

            var rows = await _context.Fees.AsNoTracking().Where(f => f.product_id == entity.product_id && f.id != id).ToListAsync();
            rows.Add(entity);
            await ValidateAsync(entity, rows);

            _audit.LogUpdate(before, await BuildDtoAsync(entity, null, null, null), "sw_fees", actingUser);

            await _context.SaveChangesAsync();
            return await BuildDtoAsync(entity, null, null, null);
        }

        public async Task DeleteAsync(long id, string actingUser, bool deleteBillers = false)
        {
            var entity = await _context.Fees.FirstOrDefaultAsync(x => x.id == id)
                ?? throw new NotFoundException($"Product fee '{id}' not found.");

            // menghapus baris default membuat baris CA "Ikuti Default" berjalan Static; itu aman
            _context.Fees.Remove(entity);
            _audit.LogDelete(entity, "sw_fees", actingUser);

            // biller disimpan per produk (tabel routing), bukan per baris fee. Hanya ikut dihapus bila
            // diminta dan tidak ada baris fee lain untuk produk ini, supaya routing baris lain tidak hilang.
            bool lastRow = !await _context.Fees.AsNoTracking().AnyAsync(f => f.product_id == entity.product_id && f.id != id);
            if (deleteBillers && lastRow)
            {
                foreach (var alt in await _context.RoutesByInstAlt.Where(a => a.inst_id == entity.product_id).ToListAsync())
                {
                    _context.RoutesByInstAlt.Remove(alt);
                    _audit.LogDelete(alt, "sw_routes_by_inst_alt", actingUser);
                }

                var primary = await _context.RoutesByInst.FirstOrDefaultAsync(r => r.inst_id == entity.product_id);
                if (primary != null)
                {
                    _context.RoutesByInst.Remove(primary);
                    _audit.LogDelete(primary, "sw_routes_by_inst", actingUser);
                }
            }

            await _context.SaveChangesAsync();
        }

        private static void Apply(SwFees e, string feeType, string? routingMode, int? staticNodeId,
            int? fixedFee, int? fixedAcq, int? fixedMer, int? fixedIss, int? fixedBil, int? fixedSwt,
            decimal? pctFee, decimal? pctAcq, decimal? pctMer, decimal? pctIss, decimal? pctBil, decimal? pctSwt)
        {
            string? mode = NullIfEmpty(routingMode);
            if (mode != null && !RoutingModes.IsValid(mode))
                throw new ValidationException($"Routing mode '{mode}' tidak dikenal.");

            // baris default selalu punya mode; kosong berarti Static
            if (mode == null && ProductRoutingRules.IsDefaultRow(e))
                mode = RoutingModes.Static;

            e.fee_type = feeType;
            e.routing_mode = mode;
            e.static_node_id = mode == RoutingModes.Static ? staticNodeId : null;
            e.fixed_fee = fixedFee;
            e.fixed_fee_acq = fixedAcq;
            e.fixed_fee_mer = fixedMer;
            e.fixed_fee_iss = fixedIss;
            e.percent_fee = pctFee;
            e.percent_fee_acq = pctAcq;
            e.percent_fee_mer = pctMer;
            e.percent_fee_iss = pctIss;
            e.percent_fee_bil = pctBil;
            e.percent_fee_swt = pctSwt;

            // mode dynamic menghitung fee biller & switch dari sharing fee biller siklus
            bool dynamic = RoutingModes.IsDynamic(mode);
            e.fixed_fee_bil = dynamic ? null : fixedBil;
            e.fixed_fee_swt = dynamic ? null : fixedSwt;
        }

        private async Task ValidateAsync(SwFees entity, List<SwFees> productRows)
        {
            var defaultRow = productRows.FirstOrDefault(ProductRoutingRules.IsDefaultRow);
            string mode = ProductRoutingRules.EffectiveMode(entity, defaultRow);

            if (!RoutingModes.IsDynamic(mode))
                ValidateStaticTotals(entity);

            var billers = await ProductRoutingRules.LoadBillersAsync(_context, entity.product_id ?? string.Empty);
            await ProductRoutingRules.EnsureConsistentAsync(_context, entity.product_id ?? string.Empty,
                productRows, billers, checkLbTotal: true);
        }

        // Static: Fixed = Loket + Mitra + Issuer + Biller + Switch harus sama dengan Total Fee;
        // Percent = komponennya dijumlah tepat 100 (persen dari Total Fee).
        private static void ValidateStaticTotals(SwFees e)
        {
            if ((e.fee_type ?? "0") == "0")
            {
                var sum = (e.fixed_fee_acq ?? 0) + (e.fixed_fee_mer ?? 0) + (e.fixed_fee_iss ?? 0) + (e.fixed_fee_bil ?? 0) + (e.fixed_fee_swt ?? 0);
                if (sum != (e.fixed_fee ?? 0))
                    throw new ValidationException(
                        $"Fee Loket + Mitra + Issuer + Biller + Switch ({sum:N0}) harus sama dengan Total Fee ({e.fixed_fee ?? 0:N0}).");
            }
            else
            {
                var sum = (e.percent_fee_acq ?? 0) + (e.percent_fee_mer ?? 0) + (e.percent_fee_iss ?? 0) + (e.percent_fee_bil ?? 0) + (e.percent_fee_swt ?? 0);
                if (sum != 100)
                    throw new ValidationException($"Total Fee Loket + Mitra + Issuer + Biller + Switch harus 100% (sekarang {sum}%).");
            }
        }

        private async Task<ProductPostpaidFeeDto> BuildDtoAsync(SwFees e, string? productName, string? merchantName, string? submerchantName)
        {
            SwFees? defaultRow = ProductRoutingRules.IsDefaultRow(e) ? e : await _context.Fees.AsNoTracking()
                .FirstOrDefaultAsync(f => f.product_id == e.product_id
                    && (f.merchant_id == null || f.merchant_id == "")
                    && (f.submerchant_id == null || f.submerchant_id == ""));
            var nodeNames = await NodeNamesAsync([e.static_node_id]);

            return ToDto(e, productName, merchantName, submerchantName, defaultRow, nodeNames);
        }

        private async Task<Dictionary<int, string>> NodeNamesAsync(IEnumerable<int?> nodeIds)
        {
            var ids = nodeIds.Where(i => i.HasValue).Select(i => i!.Value).Distinct().ToList();
            if (ids.Count == 0) return new Dictionary<int, string>();

            return await _context.Nodes.AsNoTracking()
                .Where(n => ids.Contains(n.node_id))
                .ToDictionaryAsync(n => n.node_id, n => n.node_name);
        }

        private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static ProductPostpaidFeeDto ToDto(SwFees e, string? productName, string? merchantName, string? submerchantName,
            SwFees? defaultRow, IReadOnlyDictionary<int, string> nodeNames) => new(
            e.id, e.product_id, productName, e.merchant_id, merchantName, e.submerchant_id, submerchantName,
            e.group_name, e.subgroup_name, e.fee_type ?? "0", e.fixed_fee, e.fixed_fee_acq, e.fixed_fee_iss, e.fixed_fee_swt,
            e.percent_fee, e.percent_fee_acq, e.percent_fee_iss, e.percent_fee_swt,
            e.fixed_fee_mer, e.fixed_fee_bil, e.percent_fee_mer, e.percent_fee_bil,
            e.routing_mode, e.static_node_id,
            e.static_node_id.HasValue ? nodeNames.GetValueOrDefault(e.static_node_id.Value) : null,
            ProductRoutingRules.EffectiveMode(e, defaultRow));
    }
}
