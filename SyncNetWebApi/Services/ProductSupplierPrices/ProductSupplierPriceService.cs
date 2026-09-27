using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.ProductSupplierPrices;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.ProductSupplierPrices
{
    public class ProductSupplierPriceService : IProductSupplierPriceService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public ProductSupplierPriceService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<ProductSupplierPriceDto>> GetRecordsAsync(string? filter = null)
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

        public async Task<ProductSupplierPriceDto?> GetByIdAsync(int id)
        {
            var entity = await _context.MarginSuppliers.AsNoTracking().FirstOrDefaultAsync(x => x.id == id);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<ProductSupplierPriceDto> CreateAsync(CreateProductSupplierPriceRequest request, string actingUser)
        {
            if (!await _context.Products.AsNoTracking().AnyAsync(p => p.product_code == request.BillerCode))
                throw new ValidationException($"Product '{request.BillerCode}' does not exist.");

            var margin = (request.HargaJual ?? 0) - request.HargaBeli;
            if (margin < 0)
                throw new ValidationException("Margin lebih kecil dari NOL, silahkan cek harga jual.");

            // product_name is denormalized from Product Master at save time (legacy
            // ProductMasterGetProductName), not kept in sync afterwards.
            var productName = await _context.Products.AsNoTracking()
                .Where(p => p.product_code == request.BillerCode).Select(p => p.product_name).FirstOrDefaultAsync();

            var entity = new SwMarginSupplier
            {
                supplier_id = request.SupplierId,
                biller_code = request.BillerCode,
                product_name = productName,
                denom = request.Denom,
                harga_beli = request.HargaBeli,
                harga_jual = request.HargaJual,
                margin = margin,
                status = request.Active ? "1" : "0"
            };

            _context.MarginSuppliers.Add(entity);
            _audit.LogInsert(entity, "sw_margin_supplier", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<ProductSupplierPriceDto> UpdateAsync(int id, UpdateProductSupplierPriceRequest request, string actingUser)
        {
            var entity = await _context.MarginSuppliers.FirstOrDefaultAsync(x => x.id == id)
                ?? throw new NotFoundException($"Supplier price '{id}' not found.");

            var margin = (request.HargaJual ?? 0) - request.HargaBeli;
            if (margin < 0)
                throw new ValidationException("Margin lebih kecil dari NOL, silahkan cek harga jual.");

            // product_name re-resolved from Product Master, same as legacy Save().
            var productName = await _context.Products.AsNoTracking()
                .Where(p => p.product_code == entity.biller_code).Select(p => p.product_name).FirstOrDefaultAsync();

            var before = ToDto(entity);
            entity.product_name = productName;
            entity.denom = request.Denom;
            entity.harga_beli = request.HargaBeli;
            entity.harga_jual = request.HargaJual;
            entity.margin = margin;
            entity.status = request.Active ? "1" : "0";

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

        private static ProductSupplierPriceDto ToDto(SwMarginSupplier e) => new(e.id, e.supplier_id, e.biller_code, e.product_name, e.denom, e.harga_beli, e.harga_jual, e.margin, e.status == "1");
    }
}
