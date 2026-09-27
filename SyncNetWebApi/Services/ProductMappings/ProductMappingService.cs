using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.ProductMappings;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.ProductMappings
{
    public class ProductMappingService : IProductMappingService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public ProductMappingService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<ProductMappingDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.ProductMappings.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(m => (m.source_biller_code ?? "").ToLower().Contains(f)
                    || (m.dest_biller_code ?? "").ToLower().Contains(f)
                    || (m.notes ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(m => m.id).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<ProductMappingDto?> GetByIdAsync(long id)
        {
            var entity = await _context.ProductMappings.AsNoTracking().FirstOrDefaultAsync(m => m.id == id);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<ProductMappingDto> CreateAsync(CreateProductMappingRequest request, string actingUser)
        {
            var entity = new SwProductMapping
            {
                app_name = request.AppName,
                source_biller_code = request.SourceBillerCode,
                denom = request.Denom,
                dest_biller_code = request.DestBillerCode,
                is_deposit = request.IsDeposit ? "1" : "0",
                notes = request.Notes
            };

            _context.ProductMappings.Add(entity);
            _audit.LogInsert(entity, "sw_product_mapping", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<ProductMappingDto> UpdateAsync(long id, UpdateProductMappingRequest request, string actingUser)
        {
            var entity = await _context.ProductMappings.FirstOrDefaultAsync(m => m.id == id)
                ?? throw new NotFoundException($"Product mapping '{id}' not found.");

            var before = ToDto(entity);
            entity.source_biller_code = request.SourceBillerCode;
            entity.denom = request.Denom;
            entity.dest_biller_code = request.DestBillerCode;
            entity.is_deposit = request.IsDeposit ? "1" : "0";
            entity.notes = request.Notes;

            _audit.LogUpdate(before, ToDto(entity), "sw_product_mapping", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(long id, string actingUser)
        {
            var entity = await _context.ProductMappings.FirstOrDefaultAsync(m => m.id == id)
                ?? throw new NotFoundException($"Product mapping '{id}' not found.");

            _context.ProductMappings.Remove(entity);
            _audit.LogDelete(entity, "sw_product_mapping", actingUser);

            await _context.SaveChangesAsync();
        }

        private static ProductMappingDto ToDto(SwProductMapping e) => new(e.id, e.app_name, e.source_biller_code, e.denom, e.dest_biller_code, e.is_deposit == "1", e.notes);
    }
}
