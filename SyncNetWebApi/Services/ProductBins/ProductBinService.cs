using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.ProductBins;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.ProductBins
{
    public class ProductBinService : IProductBinService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public ProductBinService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<ProductBinDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.ProductBins.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(b => (b.bin ?? "").ToLower().Contains(f) || (b.bank_name ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(b => b.id).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<ProductBinDto?> GetByIdAsync(long id)
        {
            var entity = await _context.ProductBins.AsNoTracking().FirstOrDefaultAsync(b => b.id == id);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<ProductBinDto> CreateAsync(CreateProductBinRequest request, string actingUser)
        {
            var entity = new SwProductBin
            {
                bin = request.Bin,
                cbc = request.Cbc,
                bank_name = request.BankName
            };

            _context.ProductBins.Add(entity);
            _audit.LogInsert(entity, "sw_product_bins", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<ProductBinDto> UpdateAsync(long id, UpdateProductBinRequest request, string actingUser)
        {
            var entity = await _context.ProductBins.FirstOrDefaultAsync(b => b.id == id)
                ?? throw new NotFoundException($"BIN '{id}' not found.");

            var before = new { entity.bin, entity.cbc, entity.bank_name };
            entity.bin = request.Bin;
            entity.cbc = request.Cbc;
            entity.bank_name = request.BankName;

            _audit.LogUpdate(before, new { entity.bin, entity.cbc, entity.bank_name }, "sw_product_bins", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(long id, string actingUser)
        {
            var entity = await _context.ProductBins.FirstOrDefaultAsync(b => b.id == id)
                ?? throw new NotFoundException($"BIN '{id}' not found.");

            _context.ProductBins.Remove(entity);
            _audit.LogDelete(new { entity.id, entity.bin }, "sw_product_bins", actingUser);

            await _context.SaveChangesAsync();
        }

        private static ProductBinDto ToDto(SwProductBin e) => new(e.id, e.bin, e.cbc, e.bank_name);
    }
}
