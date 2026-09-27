using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.ProductCategories;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.ProductCategories
{
    public class ProductCategoryService : IProductCategoryService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public ProductCategoryService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<ProductCategoryDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.ProductCategories.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(c => c.category.ToLower().Contains(f) || (c.notes ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(c => c.category).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<ProductCategoryDto?> GetByIdAsync(string category)
        {
            var entity = await _context.ProductCategories.AsNoTracking().FirstOrDefaultAsync(c => c.category == category);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<ProductCategoryDto> CreateAsync(CreateProductCategoryRequest request, string actingUser)
        {
            if (await _context.ProductCategories.AsNoTracking().AnyAsync(c => c.category == request.Category))
                throw new ConflictException($"Product category '{request.Category}' already exists.");

            var entity = new SwProductCategory
            {
                category = request.Category,
                is_topup = request.IsTopup ? "1" : "0",
                notes = request.Notes
            };

            _context.ProductCategories.Add(entity);
            _audit.LogInsert(entity, "sw_product_category", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<ProductCategoryDto> UpdateAsync(string category, UpdateProductCategoryRequest request, string actingUser)
        {
            var entity = await _context.ProductCategories.FirstOrDefaultAsync(c => c.category == category)
                ?? throw new NotFoundException($"Product category '{category}' not found.");

            var before = ToDto(entity);
            entity.is_topup = request.IsTopup ? "1" : "0";
            entity.notes = request.Notes;

            _audit.LogUpdate(before, ToDto(entity), "sw_product_category", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(string category, string actingUser)
        {
            var entity = await _context.ProductCategories.FirstOrDefaultAsync(c => c.category == category)
                ?? throw new NotFoundException($"Product category '{category}' not found.");

            _context.ProductCategories.Remove(entity);
            _audit.LogDelete(entity, "sw_product_category", actingUser);

            await _context.SaveChangesAsync();
        }

        private static ProductCategoryDto ToDto(SwProductCategory e) => new(e.category, e.is_topup == "1", e.notes);
    }
}
