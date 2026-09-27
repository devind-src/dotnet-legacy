using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.ProductTransfers;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.ProductTransfers
{
    public class ProductTransferService : IProductTransferService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public ProductTransferService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<ProductTransferDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.ProductTransfers.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(t => (t.name ?? "").ToLower().Contains(f)
                    || (t.bank_code ?? "").ToLower().Contains(f)
                    || (t.bank_name ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(t => t.id).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<ProductTransferDto?> GetByIdAsync(long id)
        {
            var entity = await _context.ProductTransfers.AsNoTracking().FirstOrDefaultAsync(t => t.id == id);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<ProductTransferDto> CreateAsync(CreateProductTransferRequest request, string actingUser)
        {
            // Matches legacy ProductTransferIsExist guard — case-insensitive combination check.
            var nameLower = request.Name.ToLower();
            var bankCodeLower = request.BankCode.ToLower();
            if (await _context.ProductTransfers.AsNoTracking()
                    .AnyAsync(t => (t.name ?? "").ToLower() == nameLower && (t.bank_code ?? "").ToLower() == bankCodeLower))
                throw new ConflictException($"Combination of node '{request.Name}' and bank code '{request.BankCode}' already exists.");

            // sw_product_transfer.id IS identity — do not assign manually (see entity note).
            var entity = new SwProductTransfer
            {
                name = request.Name,
                bank_code = request.BankCode,
                bank_name = request.BankName
            };

            _context.ProductTransfers.Add(entity);
            await _context.SaveChangesAsync();

            _audit.LogInsert(entity, "sw_product_transfer", actingUser);
            await _context.SaveChangesAsync();

            return ToDto(entity);
        }

        public async Task<ProductTransferDto> UpdateAsync(long id, UpdateProductTransferRequest request, string actingUser)
        {
            var entity = await _context.ProductTransfers.FirstOrDefaultAsync(t => t.id == id)
                ?? throw new NotFoundException($"Product transfer '{id}' not found.");

            var before = ToDto(entity);
            entity.bank_code = request.BankCode;
            entity.bank_name = request.BankName;

            _audit.LogUpdate(before, ToDto(entity), "sw_product_transfer", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(long id, string actingUser)
        {
            var entity = await _context.ProductTransfers.FirstOrDefaultAsync(t => t.id == id)
                ?? throw new NotFoundException($"Product transfer '{id}' not found.");

            _context.ProductTransfers.Remove(entity);
            _audit.LogDelete(entity, "sw_product_transfer", actingUser);

            await _context.SaveChangesAsync();
        }

        private static ProductTransferDto ToDto(SwProductTransfer e) => new(e.id, e.name, e.bank_code, e.bank_name);
    }
}
