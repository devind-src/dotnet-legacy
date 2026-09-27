using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.Merchants;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.Merchants
{
    public class MerchantService : IMerchantService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public MerchantService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<MerchantDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.Merchants.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(m =>
                    m.merchant_id.ToLower().Contains(f) ||
                    (m.name ?? "").ToLower().Contains(f) ||
                    (m.city ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(m => m.merchant_id).ToListAsync();
            return list.Select(m => ToDto(m, null)).ToList();
        }

        public async Task<MerchantDto?> GetByIdAsync(string merchantId)
        {
            var entity = await _context.Merchants.AsNoTracking()
                .FirstOrDefaultAsync(m => m.merchant_id == merchantId);
            if (entity == null) return null;

            var key = await _context.MerchantKeys.AsNoTracking().FirstOrDefaultAsync(k => k.merchant_id == merchantId);
            return ToDto(entity, key);
        }

        public async Task<MerchantDto> CreateAsync(CreateMerchantRequest request, string actingUser)
        {
            var exists = await _context.Merchants.AsNoTracking()
                .AnyAsync(m => m.merchant_id == request.MerchantId);
            if (exists)
                throw new ConflictException($"Merchant '{request.MerchantId}' already exists.");

            var participantExists = await _context.Participants.AsNoTracking()
                .AnyAsync(p => p.participant_id == request.ParticipantId);
            if (!participantExists)
                throw new ValidationException($"Participant '{request.ParticipantId}' does not exist.");

            var entity = new SwMerchant { merchant_id = request.MerchantId, created_by = actingUser, created_dt = System.DateTime.UtcNow };
            ApplyFields(entity, request.ParticipantId, request.MccCode, request.Name, request.Address, request.City,
                request.Zipcode, request.Phone, request.Fax, request.Email, request.Person, request.BankName,
                request.Branch, request.DestBank, request.AccNumber, request.OwnerName, request.Active,
                request.VaName, request.DateJoin, request.Brand, request.VirtualAccount, request.MidExt, request.Nmid);

            _context.Merchants.Add(entity);
            _audit.LogInsert(entity, "sw_merchant", actingUser);

            var key = new SwMerchantKey
            {
                merchant_id = request.MerchantId,
                key_length = request.KeyLength,
                pinblock_format = request.PinblockFormat,
                master_key = request.MasterKey,
                master_kcv = request.MasterKcv,
                key_under_lmk = request.KeyUnderLmk,
                key_under_zmk = request.KeyUnderZmk,
                key_check_value = request.KeyCheckValue
            };
            _context.MerchantKeys.Add(key);
            _audit.LogInsert(key, "sw_merchant_key", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity, key);
        }

        public async Task<MerchantDto> UpdateAsync(string merchantId, UpdateMerchantRequest request, string actingUser)
        {
            var entity = await _context.Merchants.FirstOrDefaultAsync(m => m.merchant_id == merchantId)
                ?? throw new NotFoundException($"Merchant '{merchantId}' not found.");

            var participantExists = await _context.Participants.AsNoTracking()
                .AnyAsync(p => p.participant_id == request.ParticipantId);
            if (!participantExists)
                throw new ValidationException($"Participant '{request.ParticipantId}' does not exist.");

            var before = ToAuditSnapshot(entity);
            ApplyFields(entity, request.ParticipantId, request.MccCode, request.Name, request.Address, request.City,
                request.Zipcode, request.Phone, request.Fax, request.Email, request.Person, request.BankName,
                request.Branch, request.DestBank, request.AccNumber, request.OwnerName, request.Active,
                request.VaName, request.DateJoin, request.Brand, request.VirtualAccount, request.MidExt, request.Nmid);
            entity.updated_by = actingUser;
            entity.updated_dt = System.DateTime.UtcNow;
            _audit.LogUpdate(before, ToAuditSnapshot(entity), "sw_merchant", actingUser);

            var key = await _context.MerchantKeys.FirstOrDefaultAsync(k => k.merchant_id == merchantId);
            if (key == null)
            {
                key = new SwMerchantKey { merchant_id = merchantId };
                _context.MerchantKeys.Add(key);
            }
            key.key_length = request.KeyLength;
            key.pinblock_format = request.PinblockFormat;
            key.master_key = request.MasterKey;
            key.master_kcv = request.MasterKcv;
            key.key_under_lmk = request.KeyUnderLmk;
            key.key_under_zmk = request.KeyUnderZmk;
            key.key_check_value = request.KeyCheckValue;

            await _context.SaveChangesAsync();
            return ToDto(entity, key);
        }

        public async Task DeleteAsync(string merchantId, string actingUser)
        {
            var entity = await _context.Merchants.FirstOrDefaultAsync(m => m.merchant_id == merchantId)
                ?? throw new NotFoundException($"Merchant '{merchantId}' not found.");

            var usedByStore = await _context.Stores.AsNoTracking().AnyAsync(s => s.merchant_id == merchantId);
            if (usedByStore)
                throw new ConflictException($"Merchant '{merchantId}' is still referenced by one or more stores and cannot be deleted.");

            var usedByTerminal = await _context.Terminals.AsNoTracking().AnyAsync(t => t.merchant_id == merchantId);
            if (usedByTerminal)
                throw new ConflictException($"Merchant '{merchantId}' is still referenced by one or more terminals and cannot be deleted.");

            var usedBySubMerchant = await _context.SubMerchants.AsNoTracking().AnyAsync(s => s.merchant_id == merchantId);
            if (usedBySubMerchant)
                throw new ConflictException($"Merchant '{merchantId}' is still referenced by one or more sub merchants and cannot be deleted.");

            var key = await _context.MerchantKeys.FirstOrDefaultAsync(k => k.merchant_id == merchantId);
            if (key != null)
            {
                _context.MerchantKeys.Remove(key);
                _audit.LogDelete(new { key.merchant_id }, "sw_merchant_key", actingUser);
            }

            _context.Merchants.Remove(entity);
            _audit.LogDelete(new { entity.merchant_id, entity.name }, "sw_merchant", actingUser);

            await _context.SaveChangesAsync();
        }

        private static void ApplyFields(SwMerchant e, string participantId, string? mccCode, string name,
            string? address, string? city, string? zipcode, string? phone, string? fax, string? email,
            string? person, string? bankName, string? branch, string? destBank, string? accNumber,
            string? ownerName, bool active, string? vaName, string? dateJoin, string? brand,
            string? virtualAccount, string? midExt, string? nmid)
        {
            e.participant_id = participantId;
            e.mcc_code = mccCode;
            e.name = name;
            e.address = address;
            e.city = city;
            e.zipcode = zipcode;
            e.phone = phone;
            e.fax = fax;
            e.email = email;
            e.person = person;
            e.bank_name = bankName;
            e.branch = branch;
            e.dest_bank = destBank;
            e.acc_number = accNumber;
            e.owner_name = ownerName;
            e.status = active ? "1" : "0";
            e.va_name = vaName;
            e.date_join = dateJoin;
            e.brand = brand;
            e.virtual_account = virtualAccount;
            e.mid_ext = midExt;
            e.nmid = nmid;
        }

        private static object ToAuditSnapshot(SwMerchant e) => new
        {
            e.participant_id, e.mcc_code, e.name, e.address, e.city, e.zipcode, e.phone, e.fax,
            e.email, e.person, e.bank_name, e.branch, e.dest_bank, e.acc_number, e.owner_name,
            e.status, e.va_name, e.date_join, e.brand, e.virtual_account, e.mid_ext, e.nmid
        };

        private static MerchantDto ToDto(SwMerchant e, SwMerchantKey? key) => new(
            e.merchant_id,
            e.participant_id,
            e.mcc_code,
            e.name,
            e.address,
            e.city,
            e.zipcode,
            e.phone,
            e.fax,
            e.email,
            e.person,
            e.bank_name,
            e.branch,
            e.dest_bank,
            e.acc_number,
            e.owner_name,
            e.status == "1",
            e.va_name,
            e.date_join,
            e.brand,
            e.virtual_account,
            e.mid_ext,
            e.nmid,
            key?.key_length,
            key?.pinblock_format,
            key?.master_key,
            key?.master_kcv,
            key?.key_under_lmk,
            key?.key_under_zmk,
            key?.key_check_value);
    }
}
