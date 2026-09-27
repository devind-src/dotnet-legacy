using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.Brands;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.Brands
{
    public class BrandService : IBrandService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public BrandService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<BrandDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.Brands.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(b => (b.brand ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(b => b.brand).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<BrandDto?> GetByIdAsync(int id)
        {
            var entity = await _context.Brands.AsNoTracking().FirstOrDefaultAsync(b => b.id == id);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<BrandDto> CreateAsync(CreateBrandRequest request, string actingUser)
        {
            var entity = new SwBrand
            {
                brand = request.Brand,
                status = "1",
                created_by = actingUser,
                created_dt = DateTime.UtcNow
            };

            _context.Brands.Add(entity);
            _audit.LogInsert(entity, "sw_brand", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<BrandDto> UpdateAsync(int id, UpdateBrandRequest request, string actingUser)
        {
            var entity = await _context.Brands.FirstOrDefaultAsync(b => b.id == id)
                ?? throw new NotFoundException($"Brand '{id}' not found.");

            var before = new { entity.brand, entity.status };
            entity.brand = request.Brand;
            entity.status = request.Active ? "1" : "0";
            entity.updated_by = actingUser;
            entity.updated_dt = DateTime.UtcNow;

            _audit.LogUpdate(before, new { entity.brand, entity.status }, "sw_brand", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(int id, string actingUser)
        {
            var entity = await _context.Brands.FirstOrDefaultAsync(b => b.id == id)
                ?? throw new NotFoundException($"Brand '{id}' not found.");

            _context.Brands.Remove(entity);
            _audit.LogDelete(new { entity.id, entity.brand }, "sw_brand", actingUser);

            await _context.SaveChangesAsync();
        }

        private static BrandDto ToDto(SwBrand e) => new(e.id, e.brand, e.status == "1");
    }
}
