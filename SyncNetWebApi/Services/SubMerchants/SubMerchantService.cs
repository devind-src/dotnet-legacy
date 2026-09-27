using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.SubMerchants;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.SubMerchants
{
    public class SubMerchantService : ISubMerchantService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public SubMerchantService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<SubMerchantDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.SubMerchants.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(s =>
                    s.submerchant_id.ToLower().Contains(f) ||
                    (s.name ?? "").ToLower().Contains(f) ||
                    (s.city ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(s => s.submerchant_id).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<SubMerchantDto?> GetByIdAsync(string subMerchantId)
        {
            var entity = await _context.SubMerchants.AsNoTracking()
                .FirstOrDefaultAsync(s => s.submerchant_id == subMerchantId);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<SubMerchantDto> CreateAsync(CreateSubMerchantRequest request, string actingUser)
        {
            var exists = await _context.SubMerchants.AsNoTracking()
                .AnyAsync(s => s.submerchant_id == request.SubMerchantId);
            if (exists)
                throw new ConflictException($"Sub merchant '{request.SubMerchantId}' already exists.");

            var merchantExists = await _context.Merchants.AsNoTracking()
                .AnyAsync(m => m.merchant_id == request.MerchantId);
            if (!merchantExists)
                throw new ValidationException($"Merchant '{request.MerchantId}' does not exist.");

            var entity = new SwSubMerchant
            {
                submerchant_id = request.SubMerchantId,
                submerchant_id_ext = request.SubMerchantIdExt,
                merchant_id = request.MerchantId,
                name = request.Name,
                address = request.Address,
                city = request.City,
                zipcode = request.Zipcode,
                phone = request.Phone,
                fax = request.Fax,
                email = request.Email,
                status = request.Active ? "1" : "0",
                date_join = request.DateJoin,
                created_by = actingUser,
                created_dt = System.DateTime.UtcNow
            };

            _context.SubMerchants.Add(entity);
            _audit.LogInsert(entity, "sw_submerchant", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<SubMerchantDto> UpdateAsync(string subMerchantId, UpdateSubMerchantRequest request, string actingUser)
        {
            var entity = await _context.SubMerchants.FirstOrDefaultAsync(s => s.submerchant_id == subMerchantId)
                ?? throw new NotFoundException($"Sub merchant '{subMerchantId}' not found.");

            var merchantExists = await _context.Merchants.AsNoTracking()
                .AnyAsync(m => m.merchant_id == request.MerchantId);
            if (!merchantExists)
                throw new ValidationException($"Merchant '{request.MerchantId}' does not exist.");

            var before = ToAuditSnapshot(entity);

            entity.submerchant_id_ext = request.SubMerchantIdExt;
            entity.merchant_id = request.MerchantId;
            entity.name = request.Name;
            entity.address = request.Address;
            entity.city = request.City;
            entity.zipcode = request.Zipcode;
            entity.phone = request.Phone;
            entity.fax = request.Fax;
            entity.email = request.Email;
            entity.status = request.Active ? "1" : "0";
            entity.date_join = request.DateJoin;
            entity.updated_by = actingUser;
            entity.updated_dt = System.DateTime.UtcNow;

            _audit.LogUpdate(before, ToAuditSnapshot(entity), "sw_submerchant", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(string subMerchantId, string actingUser)
        {
            var entity = await _context.SubMerchants.FirstOrDefaultAsync(s => s.submerchant_id == subMerchantId)
                ?? throw new NotFoundException($"Sub merchant '{subMerchantId}' not found.");

            var usedByTerminal = await _context.Terminals.AsNoTracking().AnyAsync(t => t.submerchant_id == subMerchantId);
            if (usedByTerminal)
                throw new ConflictException($"Sub merchant '{subMerchantId}' is still referenced by one or more terminals and cannot be deleted.");

            _context.SubMerchants.Remove(entity);
            _audit.LogDelete(new { entity.submerchant_id, entity.name }, "sw_submerchant", actingUser);

            await _context.SaveChangesAsync();
        }

        private static object ToAuditSnapshot(SwSubMerchant e) => new
        {
            e.submerchant_id_ext, e.merchant_id, e.name, e.address, e.city, e.zipcode,
            e.phone, e.fax, e.email, e.status, e.date_join
        };

        private static SubMerchantDto ToDto(SwSubMerchant e) => new(
            e.submerchant_id,
            e.submerchant_id_ext,
            e.merchant_id,
            e.name,
            e.address,
            e.city,
            e.zipcode,
            e.phone,
            e.fax,
            e.email,
            e.status == "1",
            e.date_join);
    }
}
