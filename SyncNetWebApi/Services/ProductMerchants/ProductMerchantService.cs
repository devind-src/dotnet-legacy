using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.ProductMerchants;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.ProductMerchants
{
    public class ProductMerchantService : IProductMerchantService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public ProductMerchantService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        // Mirrors legacy MerchantProductGetRecords — an INNER JOIN against sw_merchant and
        // sw_product, so a row whose merchant/product no longer exists silently drops out of
        // the list (same as legacy; there is no FK constraint to prevent that from happening).
        public async Task<IReadOnlyList<ProductMerchantDto>> GetRecordsAsync(string? filter = null)
        {
            var query = from mp in _context.MerchantProducts.AsNoTracking()
                        join m in _context.Merchants.AsNoTracking() on mp.merchant_id equals m.merchant_id
                        join p in _context.Products.AsNoTracking() on mp.product_id equals p.product_code
                        select new { mp, m.name, ProductName = p.product_name, p.category };

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(x => (x.name ?? "").ToLower().Contains(f)
                    || (x.ProductName ?? "").ToLower().Contains(f)
                    || (x.mp.notes ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(x => x.mp.id).ToListAsync();
            return list.Select(x => new ProductMerchantDto(x.mp.id, x.mp.merchant_id, x.name, x.mp.product_id, x.ProductName, x.category, x.mp.notes, x.mp.status == "1")).ToList();
        }

        public async Task<ProductMerchantDto?> GetByIdAsync(int id)
        {
            var entity = await _context.MerchantProducts.AsNoTracking().FirstOrDefaultAsync(mp => mp.id == id);
            if (entity == null) return null;

            var merchantName = await _context.Merchants.AsNoTracking()
                .Where(m => m.merchant_id == entity.merchant_id).Select(m => m.name).FirstOrDefaultAsync();
            var product = await _context.Products.AsNoTracking()
                .Where(p => p.product_code == entity.product_id).Select(p => new { p.product_name, p.category }).FirstOrDefaultAsync();

            return ToDto(entity, merchantName, product?.product_name, product?.category);
        }

        public async Task<ProductMerchantDto> CreateAsync(CreateProductMerchantRequest request, string actingUser)
        {
            if (!await _context.Merchants.AsNoTracking().AnyAsync(m => m.merchant_id == request.MerchantId))
                throw new ValidationException($"Merchant '{request.MerchantId}' does not exist.");

            if (!await _context.Products.AsNoTracking().AnyAsync(p => p.product_code == request.ProductId))
                throw new ValidationException($"Product '{request.ProductId}' does not exist.");

            var entity = new SwMerchantProduct
            {
                merchant_id = request.MerchantId,
                product_id = request.ProductId,
                notes = request.Notes,
                status = request.Active ? "1" : "0"
            };

            _context.MerchantProducts.Add(entity);
            _audit.LogInsert(entity, "sw_merchant_product", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity, null, null, null);
        }

        public async Task<ProductMerchantDto> UpdateAsync(int id, UpdateProductMerchantRequest request, string actingUser)
        {
            var entity = await _context.MerchantProducts.FirstOrDefaultAsync(mp => mp.id == id)
                ?? throw new NotFoundException($"Product merchant '{id}' not found.");

            if (!await _context.Products.AsNoTracking().AnyAsync(p => p.product_code == request.ProductId))
                throw new ValidationException($"Product '{request.ProductId}' does not exist.");

            var before = ToDto(entity, null, null, null);
            entity.product_id = request.ProductId;
            entity.notes = request.Notes;
            entity.status = request.Active ? "1" : "0";

            _audit.LogUpdate(before, ToDto(entity, null, null, null), "sw_merchant_product", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity, null, null, null);
        }

        public async Task DeleteAsync(int id, string actingUser)
        {
            var entity = await _context.MerchantProducts.FirstOrDefaultAsync(mp => mp.id == id)
                ?? throw new NotFoundException($"Product merchant '{id}' not found.");

            _context.MerchantProducts.Remove(entity);
            _audit.LogDelete(entity, "sw_merchant_product", actingUser);

            await _context.SaveChangesAsync();
        }

        private static ProductMerchantDto ToDto(SwMerchantProduct e, string? merchantName, string? productName, string? category)
            => new(e.id, e.merchant_id, merchantName, e.product_id, productName, category, e.notes, e.status == "1");
    }
}
