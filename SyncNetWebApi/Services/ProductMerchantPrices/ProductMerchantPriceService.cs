using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.ProductMerchantPrices;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.ProductMerchantPrices
{
    public class ProductMerchantPriceService : IProductMerchantPriceService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public ProductMerchantPriceService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        // Mirrors legacy ProductMarginMerchantGetRecords — an INNER JOIN against sw_merchant,
        // same fidelity choice as ProductMerchantService (Phase P2).
        public async Task<IReadOnlyList<ProductMerchantPriceDto>> GetRecordsAsync(string? filter = null)
        {
            var query = from pm in _context.MarginMerchants.AsNoTracking()
                        join m in _context.Merchants.AsNoTracking() on pm.merchant_id equals m.merchant_id
                        select new { pm, m.name };

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(x => (x.name ?? "").ToLower().Contains(f)
                    || (x.pm.biller_code ?? "").ToLower().Contains(f)
                    || (x.pm.product_name ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(x => x.pm.id).ToListAsync();
            return list.Select(x => new ProductMerchantPriceDto(x.pm.id, x.pm.merchant_id, x.name, x.pm.biller_code, x.pm.product_name, x.pm.denom, x.pm.harga_jual)).ToList();
        }

        public async Task<ProductMerchantPriceDto?> GetByIdAsync(int id)
        {
            var entity = await _context.MarginMerchants.AsNoTracking().FirstOrDefaultAsync(x => x.id == id);
            if (entity == null) return null;

            var merchantName = await _context.Merchants.AsNoTracking()
                .Where(m => m.merchant_id == entity.merchant_id).Select(m => m.name).FirstOrDefaultAsync();
            return ToDto(entity, merchantName);
        }

        public async Task<ProductMerchantPriceDto> CreateAsync(CreateProductMerchantPriceRequest request, string actingUser)
        {
            if (!await _context.Merchants.AsNoTracking().AnyAsync(m => m.merchant_id == request.MerchantId))
                throw new ValidationException($"Merchant '{request.MerchantId}' does not exist.");

            if (!await _context.Products.AsNoTracking().AnyAsync(p => p.product_code == request.BillerCode))
                throw new ValidationException($"Product '{request.BillerCode}' does not exist.");

            var productName = await _context.Products.AsNoTracking()
                .Where(p => p.product_code == request.BillerCode).Select(p => p.product_name).FirstOrDefaultAsync();

            var entity = new SwMarginMerchant
            {
                merchant_id = request.MerchantId,
                biller_code = request.BillerCode,
                product_name = productName,
                denom = request.Denom,
                harga_jual = request.HargaJual,
                // No legacy form ever exposes Status for this module — always "1" on create,
                // matching the legacy constructor default, never touched by Update.
                status = "1"
            };

            _context.MarginMerchants.Add(entity);
            _audit.LogInsert(entity, "sw_margin_merchant", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity, null);
        }

        public async Task<ProductMerchantPriceDto> UpdateAsync(int id, UpdateProductMerchantPriceRequest request, string actingUser)
        {
            var entity = await _context.MarginMerchants.FirstOrDefaultAsync(x => x.id == id)
                ?? throw new NotFoundException($"Merchant price '{id}' not found.");

            var productName = await _context.Products.AsNoTracking()
                .Where(p => p.product_code == entity.biller_code).Select(p => p.product_name).FirstOrDefaultAsync();

            var before = ToDto(entity, null);
            entity.product_name = productName;
            entity.denom = request.Denom;
            entity.harga_jual = request.HargaJual;

            _audit.LogUpdate(before, ToDto(entity, null), "sw_margin_merchant", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity, null);
        }

        public async Task DeleteAsync(int id, string actingUser)
        {
            var entity = await _context.MarginMerchants.FirstOrDefaultAsync(x => x.id == id)
                ?? throw new NotFoundException($"Merchant price '{id}' not found.");

            _context.MarginMerchants.Remove(entity);
            _audit.LogDelete(entity, "sw_margin_merchant", actingUser);

            await _context.SaveChangesAsync();
        }

        private static ProductMerchantPriceDto ToDto(SwMarginMerchant e, string? merchantName) => new(e.id, e.merchant_id, merchantName, e.biller_code, e.product_name, e.denom, e.harga_jual);
    }
}
