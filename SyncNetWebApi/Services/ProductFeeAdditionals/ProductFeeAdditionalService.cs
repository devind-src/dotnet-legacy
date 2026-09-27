using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.ProductFeeAdditionals;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.ProductFeeAdditionals
{
    public class ProductFeeAdditionalService : IProductFeeAdditionalService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public ProductFeeAdditionalService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<ProductFeeAdditionalDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.FeeAdditionals.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(x => (x.group_name ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(x => x.id).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<ProductFeeAdditionalDto?> GetByIdAsync(long id)
        {
            var entity = await _context.FeeAdditionals.AsNoTracking().FirstOrDefaultAsync(x => x.id == id);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<ProductFeeAdditionalDto> CreateAsync(CreateProductFeeAdditionalRequest request, string actingUser)
        {
            // id is GENERATED ALWAYS AS IDENTITY — do not assign manually (see entity note).
            var entity = new SwFeeAdditional
            {
                group_name = request.GroupName,
                fee = request.Fee
            };

            _context.FeeAdditionals.Add(entity);
            await _context.SaveChangesAsync();

            _audit.LogInsert(entity, "sw_fee_additional", actingUser);
            await _context.SaveChangesAsync();

            return ToDto(entity);
        }

        public async Task<ProductFeeAdditionalDto> UpdateAsync(long id, UpdateProductFeeAdditionalRequest request, string actingUser)
        {
            var entity = await _context.FeeAdditionals.FirstOrDefaultAsync(x => x.id == id)
                ?? throw new NotFoundException($"Additional fee '{id}' not found.");

            var before = ToDto(entity);
            entity.fee = request.Fee;

            _audit.LogUpdate(before, ToDto(entity), "sw_fee_additional", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(long id, string actingUser)
        {
            var entity = await _context.FeeAdditionals.FirstOrDefaultAsync(x => x.id == id)
                ?? throw new NotFoundException($"Additional fee '{id}' not found.");

            _context.FeeAdditionals.Remove(entity);
            _audit.LogDelete(entity, "sw_fee_additional", actingUser);

            await _context.SaveChangesAsync();
        }

        private static ProductFeeAdditionalDto ToDto(SwFeeAdditional e) => new(e.id, e.group_name, e.fee);
    }
}
