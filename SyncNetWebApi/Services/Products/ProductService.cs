using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.Products;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.Products
{
    public class ProductService : IProductService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public ProductService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<ProductDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.Products.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(p => p.product_code.ToLower().Contains(f)
                    || (p.product_name ?? "").ToLower().Contains(f)
                    || (p.category ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(p => p.product_code).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<ProductDto?> GetByIdAsync(string productCode)
        {
            var entity = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.product_code == productCode);
            return entity == null ? null : ToDto(entity);
        }

        public Task<bool> ExistsAsync(string productCode) =>
            _context.Products.AsNoTracking().AnyAsync(p => p.product_code == productCode);

        public async Task<ProductDto> CreateAsync(CreateProductRequest request, string actingUser)
        {
            var exists = await _context.Products.AsNoTracking().AnyAsync(p => p.product_code == request.ProductCode);
            if (exists)
                throw new ConflictException($"Product '{request.ProductCode}' already exists.");

            var entity = new SwProduct
            {
                product_code = request.ProductCode,
                product_name = request.ProductName,
                category = request.Category,
                status = "1",
                created_by = actingUser,
                created_dt = DateTime.UtcNow
            };

            _context.Products.Add(entity);
            _audit.LogInsert(entity, "sw_product", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<ProductDto> UpdateAsync(string productCode, UpdateProductRequest request, string actingUser)
        {
            var entity = await _context.Products.FirstOrDefaultAsync(p => p.product_code == productCode)
                ?? throw new NotFoundException($"Product '{productCode}' not found.");

            var before = new { entity.product_name, entity.category, entity.status };
            entity.product_name = request.ProductName;
            entity.category = request.Category;
            entity.status = request.Active ? "1" : "0";
            entity.updated_by = actingUser;
            entity.updated_dt = DateTime.UtcNow;

            _audit.LogUpdate(before, new { entity.product_name, entity.category, entity.status }, "sw_product", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(string productCode, string actingUser)
        {
            var entity = await _context.Products.FirstOrDefaultAsync(p => p.product_code == productCode)
                ?? throw new NotFoundException($"Product '{productCode}' not found.");

            _context.Products.Remove(entity);
            _audit.LogDelete(new { entity.product_code, entity.product_name }, "sw_product", actingUser);

            await _context.SaveChangesAsync();
        }

        private static ProductDto ToDto(SwProduct e) => new(e.product_code, e.product_name, e.category, e.status == "1");
    }
}
