using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.Stores;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.Stores
{
    public class StoreService : IStoreService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public StoreService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<StoreDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.Stores.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(s =>
                    s.store_id.ToLower().Contains(f) ||
                    (s.name ?? "").ToLower().Contains(f) ||
                    (s.city ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(s => s.store_id).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<StoreDto?> GetByIdAsync(string storeId)
        {
            var entity = await _context.Stores.AsNoTracking()
                .FirstOrDefaultAsync(s => s.store_id == storeId);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<StoreDto> CreateAsync(CreateStoreRequest request, string actingUser)
        {
            var exists = await _context.Stores.AsNoTracking()
                .AnyAsync(s => s.store_id == request.StoreId);
            if (exists)
                throw new ConflictException($"Store '{request.StoreId}' already exists.");

            var merchantExists = await _context.Merchants.AsNoTracking()
                .AnyAsync(m => m.merchant_id == request.MerchantId);
            if (!merchantExists)
                throw new ValidationException($"Merchant '{request.MerchantId}' does not exist.");

            var entity = new SwStore
            {
                store_id = request.StoreId,
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

            _context.Stores.Add(entity);
            _audit.LogInsert(entity, "sw_store", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<StoreDto> UpdateAsync(string storeId, UpdateStoreRequest request, string actingUser)
        {
            var entity = await _context.Stores.FirstOrDefaultAsync(s => s.store_id == storeId)
                ?? throw new NotFoundException($"Store '{storeId}' not found.");

            var merchantExists = await _context.Merchants.AsNoTracking()
                .AnyAsync(m => m.merchant_id == request.MerchantId);
            if (!merchantExists)
                throw new ValidationException($"Merchant '{request.MerchantId}' does not exist.");

            var before = ToAuditSnapshot(entity);

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

            _audit.LogUpdate(before, ToAuditSnapshot(entity), "sw_store", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(string storeId, string actingUser)
        {
            var entity = await _context.Stores.FirstOrDefaultAsync(s => s.store_id == storeId)
                ?? throw new NotFoundException($"Store '{storeId}' not found.");

            var usedByTerminal = await _context.Terminals.AsNoTracking().AnyAsync(t => t.store_id == storeId);
            if (usedByTerminal)
                throw new ConflictException($"Store '{storeId}' is still referenced by one or more terminals and cannot be deleted.");

            _context.Stores.Remove(entity);
            _audit.LogDelete(new { entity.store_id, entity.name }, "sw_store", actingUser);

            await _context.SaveChangesAsync();
        }

        private static object ToAuditSnapshot(SwStore e) => new
        {
            e.merchant_id, e.name, e.address, e.city, e.zipcode, e.phone, e.fax, e.email, e.status, e.date_join
        };

        private static StoreDto ToDto(SwStore e) => new(
            e.store_id,
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
