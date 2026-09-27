using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Data;
using SyncNetApi.Dtos.Lookups;

namespace SyncNetApi.Controllers
{
    /// <summary>Read-only dropdown data for tables that belong to modules outside PosBase's
    /// scope (Card &gt; Group backs BIN's "Group BIN" field, Configuration &gt; Base &gt; City backs
    /// SoundBox's "City" field) — see HANDOFF_PHASE4_TERMINAL_MANAGEMENT.md decision log.
    /// No create/update/delete here; those modules are out of scope.
    /// <br/><br/>
    /// [Authorize] (no Roles=) — corrected 2026-09-09 alongside PermissionGatedControllerBase:
    /// any logged-in user can read. This controller backs dropdowns on forms in the 63
    /// controllers that inherit that base class, so it has to stay in lockstep with it —
    /// leaving this admin-only would mean a non-admin user could open a form (now reachable)
    /// but find its dropdowns empty.</summary>
    [ApiController]
    [Authorize]
    [Route("api/v1/lookups")]
    public class LookupsController : ControllerBase
    {
        private readonly SyncNetDbContext _context;

        public LookupsController(SyncNetDbContext context)
        {
            _context = context;
        }

        [HttpGet("card-groups")]
        public async Task<ActionResult<IReadOnlyList<LookupItemDto>>> GetCardGroups()
        {
            var items = await _context.CardGroups.AsNoTracking()
                .OrderBy(g => g.group_name)
                .Select(g => new LookupItemDto(g.group_id.ToString(), g.group_name ?? g.group_id.ToString()))
                .ToListAsync();
            return Ok(items);
        }

        [HttpGet("cities")]
        public async Task<ActionResult<IReadOnlyList<LookupItemDto>>> GetCities()
        {
            var items = await _context.Cities.AsNoTracking()
                .OrderBy(c => c.city)
                .Select(c => new LookupItemDto(c.city ?? c.id.ToString(), c.city ?? c.id.ToString()))
                .ToListAsync();
            return Ok(items);
        }

        [HttpGet("banks")]
        public async Task<ActionResult<IReadOnlyList<LookupItemDto>>> GetBanks()
        {
            var items = await _context.Banks.AsNoTracking()
                .OrderBy(b => b.bank)
                .Select(b => new LookupItemDto(b.bank ?? b.id.ToString(), b.bank ?? b.id.ToString()))
                .ToListAsync();
            return Ok(items);
        }

        [HttpGet("brands")]
        public async Task<ActionResult<IReadOnlyList<LookupItemDto>>> GetBrands()
        {
            var items = await _context.Brands.AsNoTracking()
                .OrderBy(b => b.brand)
                .Select(b => new LookupItemDto(b.brand ?? b.id.ToString(), b.brand ?? b.id.ToString()))
                .ToListAsync();
            return Ok(items);
        }

        [HttpGet("va-accounts")]
        public async Task<ActionResult<IReadOnlyList<LookupItemDto>>> GetVaAccounts()
        {
            var items = await _context.VaAccounts.AsNoTracking()
                .OrderBy(v => v.va_name)
                .Select(v => new LookupItemDto(v.acc_nr, v.va_name ?? v.acc_nr))
                .ToListAsync();
            return Ok(items);
        }

        [HttpGet("mcc-codes")]
        public async Task<ActionResult<IReadOnlyList<LookupItemDto>>> GetMccCodes()
        {
            var items = await _context.MccCodes.AsNoTracking()
                .OrderBy(m => m.mcc_code)
                .Select(m => new LookupItemDto(m.mcc_code, m.mcc_desc == null ? m.mcc_code : $"{m.mcc_code} - {m.mcc_desc}"))
                .ToListAsync();
            return Ok(items);
        }

        [HttpGet("merchant-criteria")]
        public async Task<ActionResult<IReadOnlyList<LookupItemDto>>> GetMerchantCriteria()
        {
            var items = await _context.MerchantCriteria.AsNoTracking()
                .Select(c => c.criteria)
                .Distinct()
                .OrderBy(c => c)
                .Select(c => new LookupItemDto(c ?? string.Empty, c ?? string.Empty))
                .ToListAsync();
            return Ok(items);
        }

        /// <summary>Products not yet assigned a Routing &gt; Product rule — backs that page's
        /// suggestion datalist (mirrors legacy ProductMasterGetProductRouting). A product
        /// missing from this list can still be typed manually; the routing rule create
        /// endpoint is the actual source of truth for duplicates.</summary>
        [HttpGet("products-available-for-routing")]
        public async Task<ActionResult<IReadOnlyList<LookupItemDto>>> GetProductsAvailableForRouting()
        {
            // Routing > Product hanya untuk produk kategori bill payment: kategori topup (is_topup = "1") disaring
            var items = await (from p in _context.Products.AsNoTracking()
                               join c in _context.ProductCategories.AsNoTracking() on p.category equals c.category into cats
                               from c in cats.DefaultIfEmpty()
                               where (c == null || c.is_topup != "1")
                                     && !_context.RoutesByInst.Any(r => r.inst_id == p.product_code)
                               orderby p.product_name
                               select new LookupItemDto(p.product_code, p.product_name ?? p.product_code))
                .ToListAsync();
            return Ok(items);
        }

        /// <summary>Produk Bill Payment &amp; Purchase (kategori bukan topup) — backs Product &gt; Fees &gt;
        /// Product Fees product picker (fase 1: Product Fees hanya untuk produk non-topup).</summary>
        [HttpGet("products-non-topup")]
        public async Task<ActionResult<IReadOnlyList<LookupItemDto>>> GetNonTopupProducts()
        {
            var items = await (from p in _context.Products.AsNoTracking()
                               join c in _context.ProductCategories.AsNoTracking() on p.category equals c.category into cats
                               from c in cats.DefaultIfEmpty()
                               where c == null || c.is_topup != "1"
                               orderby p.product_name
                               select new LookupItemDto(p.product_code, p.product_name ?? p.product_code))
                .ToListAsync();
            return Ok(items);
        }

        /// <summary>Products whose category is flagged `is_topup = "1"` — backs Routing &gt;
        /// Dynamic's suggestion datalist (mirrors legacy ProductMasterGetProductTopup).
        /// Optional `category` narrows to a single category — backs Routing &gt; Margin's
        /// per-category product checklist.</summary>
        [HttpGet("products-topup")]
        public async Task<ActionResult<IReadOnlyList<LookupItemDto>>> GetTopupProducts([FromQuery] string? category)
        {
            var items = await (from p in _context.Products.AsNoTracking()
                                join c in _context.ProductCategories.AsNoTracking() on p.category equals c.category
                                where c.is_topup == "1" && (category == null || p.category == category)
                                orderby p.product_name descending
                                select new LookupItemDto(p.product_code, p.product_name ?? p.product_code))
                .ToListAsync();
            return Ok(items);
        }

        /// <summary>Categories flagged `is_topup = "1"` — backs Routing &gt; Margin's category
        /// picker (only topup categories can have a margin route, same rule as products-topup).</summary>
        [HttpGet("categories-topup")]
        public async Task<ActionResult<IReadOnlyList<LookupItemDto>>> GetTopupCategories()
        {
            var items = await _context.ProductCategories.AsNoTracking()
                .Where(c => c.is_topup == "1")
                .OrderBy(c => c.category)
                .Select(c => new LookupItemDto(c.category, c.category))
                .ToListAsync();
            return Ok(items);
        }

        /// <summary>Biller yang dipakai routing dinamis (primary/alternate bill payment, supplier topup) —
        /// Key = node_id, Value = node_name. Backs Routing &gt; Jadwal Routing biller picker (Fase 3).</summary>
        [HttpGet("routing-billers")]
        public async Task<ActionResult<IReadOnlyList<LookupItemDto>>> GetRoutingBillers()
        {
            var suppliers = _context.MarginSuppliers.AsNoTracking().Select(s => s.supplier_id);
            var items = await _context.Nodes.AsNoTracking()
                .Where(n => _context.RoutesByInst.Any(r => r.node_id == n.node_id)
                         || _context.RoutesByInstAlt.Any(r => r.node_id == n.node_id)
                         || suppliers.Contains(n.node_name))
                .OrderBy(n => n.node_name)
                .Select(n => new LookupItemDto(n.node_id.ToString(), n.node_name))
                .ToListAsync();
            return Ok(items);
        }

        /// <summary>Produk yang punya route dinamis: bill payment (Routing &gt; Product) dan topup
        /// (Routing &gt; Margin). Value = "nama (Bill Payment|Topup)". Backs Jadwal Routing product picker.</summary>
        [HttpGet("routing-products")]
        public async Task<ActionResult<IReadOnlyList<LookupItemDto>>> GetRoutingProducts()
        {
            var bill = await (from r in _context.RoutesByInst.AsNoTracking()
                              join p in _context.Products.AsNoTracking() on r.inst_id equals p.product_code into ps
                              from p in ps.DefaultIfEmpty()
                              select new { Code = r.inst_id, Name = p != null ? p.product_name : null, Type = "Bill Payment" }).ToListAsync();
            var topup = await (from r in _context.RoutesMargin.AsNoTracking()
                               join p in _context.Products.AsNoTracking() on r.inst_id equals p.product_code into ps
                               from p in ps.DefaultIfEmpty()
                               select new { Code = r.inst_id, Name = p != null ? p.product_name : null, Type = "Topup" }).ToListAsync();

            var items = bill.Concat(topup)
                .GroupBy(x => x.Code)
                .Select(g => new LookupItemDto(g.Key, $"{g.First().Name ?? g.Key} ({string.Join(", ", g.Select(x => x.Type).Distinct())})"))
                .OrderBy(x => x.Key)
                .ToList();
            return Ok(items);
        }

        /// <summary>Distinct application names that have at least one Node with category "0"
        /// or "2" (Both/Biller-Issuer) — backs Product &gt; Mapping's "Application" dropdown
        /// (mirrors legacy AppGetAppBiller).</summary>
        [HttpGet("apps-with-biller-node")]
        public async Task<ActionResult<IReadOnlyList<LookupItemDto>>> GetAppsWithBillerNode()
        {
            var items = await (from n in _context.Nodes.AsNoTracking()
                                join a in _context.Apps.AsNoTracking() on n.app_name equals a.app_name
                                where n.category == "0" || n.category == "2"
                                select a.app_name)
                .Distinct()
                .OrderBy(name => name)
                .Select(name => new LookupItemDto(name, name))
                .ToListAsync();
            return Ok(items);
        }
    }
}
