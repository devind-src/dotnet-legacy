using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.Terminals;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.Terminals
{
    public class TerminalService : ITerminalService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public TerminalService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<TerminalDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.Terminals.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(t =>
                    t.term_id.ToLower().Contains(f) ||
                    (t.loket_name ?? "").ToLower().Contains(f) ||
                    (t.serial_number ?? "").ToLower().Contains(f) ||
                    (t.city ?? "").ToLower().Contains(f));
            }

            var terminals = await query.OrderBy(t => t.term_id).ToListAsync();
            var merchantNames = await GetMerchantNameMapAsync(terminals.Select(t => t.merchant_id));

            return terminals.Select(t => ToDto(t, null, null, merchantNames)).ToList();
        }

        public async Task<TerminalDto?> GetByIdAsync(string termId)
        {
            var entity = await _context.Terminals.AsNoTracking().FirstOrDefaultAsync(t => t.term_id == termId);
            if (entity == null) return null;

            var key = await _context.TerminalKeys.AsNoTracking().FirstOrDefaultAsync(k => k.term_id == termId);
            var init = await _context.TerminalInits.AsNoTracking().FirstOrDefaultAsync(i => i.term_id == termId);
            var merchantNames = await GetMerchantNameMapAsync(new[] { entity.merchant_id });

            return ToDto(entity, key, init, merchantNames);
        }

        public async Task<TerminalDto> CreateAsync(CreateTerminalRequest request, string actingUser)
        {
            var exists = await _context.Terminals.AsNoTracking().AnyAsync(t => t.term_id == request.TermId);
            if (exists)
                throw new ConflictException($"Terminal '{request.TermId}' already exists.");

            await ValidateReferencesAsync(request.MerchantId, request.SubMerchantId, request.StoreId);

            var snExists = await _context.Terminals.AsNoTracking().AnyAsync(t => t.serial_number == request.SerialNumber);
            if (snExists)
                throw new ConflictException($"Serial number '{request.SerialNumber}' is already in use by another terminal.");

            if (!string.IsNullOrEmpty(request.Nmid))
            {
                var nmidExists = await _context.Terminals.AsNoTracking().AnyAsync(t => t.nmid == request.Nmid);
                if (nmidExists)
                    throw new ConflictException($"NMID '{request.Nmid}' is already in use by another terminal.");
            }

            var entity = new SwTerminal { term_id = request.TermId, created_by = actingUser, created_dt = System.DateTime.UtcNow };
            ApplyGeneralFields(entity, request.MerchantId, request.SubMerchantId, request.StoreId, request.LoketName,
                request.Location, request.City, request.Phone, request.Active, request.GroupName, request.SubgroupName,
                request.Criteria, request.Nmid, request.Mpan, request.ChatId, request.Brand, request.Type,
                request.SerialNumber, request.Security, request.IpAddress, request.MacAddress, request.BankName,
                request.BankBranch, request.BankCode, request.BankAccNumber, request.BankAccName,
                request.VirtualAccount, request.VaName);

            _context.Terminals.Add(entity);
            _audit.LogInsert(entity, "sw_terminal", actingUser);

            var init = new SwTerminalInit
            {
                term_id = request.TermId,
                init_id = request.InitId,
                enable_init = request.EnableInit ? "1" : "0"
            };
            _context.TerminalInits.Add(init);
            _audit.LogInsert(init, "sw_term_init", actingUser);

            var key = new SwTerminalKey
            {
                term_id = request.TermId,
                key_length = request.KeyLength,
                pinblock_format = request.PinblockFormat,
                master_key = request.MasterKey,
                master_kcv = request.MasterKcv,
                key_under_lmk = request.KeyUnderLmk,
                key_under_zmk = request.KeyUnderZmk,
                key_check_value = request.KeyCheckValue
            };
            _context.TerminalKeys.Add(key);
            _audit.LogInsert(key, "sw_term_key", actingUser);

            await _context.SaveChangesAsync();

            var merchantNames = await GetMerchantNameMapAsync(new[] { entity.merchant_id });
            return ToDto(entity, key, init, merchantNames);
        }

        public async Task<TerminalDto> UpdateAsync(string termId, UpdateTerminalRequest request, string actingUser)
        {
            var entity = await _context.Terminals.FirstOrDefaultAsync(t => t.term_id == termId)
                ?? throw new NotFoundException($"Terminal '{termId}' not found.");

            await ValidateReferencesAsync(request.MerchantId, request.SubMerchantId, request.StoreId);

            var snExists = await _context.Terminals.AsNoTracking()
                .AnyAsync(t => t.serial_number == request.SerialNumber && t.term_id != termId);
            if (snExists)
                throw new ConflictException($"Serial number '{request.SerialNumber}' is already in use by another terminal.");

            if (!string.IsNullOrEmpty(request.Nmid))
            {
                var nmidExists = await _context.Terminals.AsNoTracking()
                    .AnyAsync(t => t.nmid == request.Nmid && t.term_id != termId);
                if (nmidExists)
                    throw new ConflictException($"NMID '{request.Nmid}' is already in use by another terminal.");
            }

            var before = ToAuditSnapshot(entity);
            ApplyGeneralFields(entity, request.MerchantId, request.SubMerchantId, request.StoreId, request.LoketName,
                request.Location, request.City, request.Phone, request.Active, request.GroupName, request.SubgroupName,
                request.Criteria, request.Nmid, request.Mpan, request.ChatId, request.Brand, request.Type,
                request.SerialNumber, request.Security, request.IpAddress, request.MacAddress, request.BankName,
                request.BankBranch, request.BankCode, request.BankAccNumber, request.BankAccName,
                request.VirtualAccount, request.VaName);
            entity.updated_by = actingUser;
            entity.updated_dt = System.DateTime.UtcNow;
            _audit.LogUpdate(before, ToAuditSnapshot(entity), "sw_terminal", actingUser);

            var init = await _context.TerminalInits.FirstOrDefaultAsync(i => i.term_id == termId);
            if (init == null)
            {
                init = new SwTerminalInit { term_id = termId };
                _context.TerminalInits.Add(init);
            }
            init.init_id = request.InitId;
            init.enable_init = request.EnableInit ? "1" : "0";

            var key = await _context.TerminalKeys.FirstOrDefaultAsync(k => k.term_id == termId);
            if (key == null)
            {
                key = new SwTerminalKey { term_id = termId };
                _context.TerminalKeys.Add(key);
            }
            key.key_length = request.KeyLength;
            key.pinblock_format = request.PinblockFormat;
            key.master_key = request.MasterKey;
            key.master_kcv = request.MasterKcv;
            key.key_under_lmk = request.KeyUnderLmk;
            key.key_under_zmk = request.KeyUnderZmk;
            key.key_check_value = request.KeyCheckValue;

            await _context.SaveChangesAsync();

            var merchantNames = await GetMerchantNameMapAsync(new[] { entity.merchant_id });
            return ToDto(entity, key, init, merchantNames);
        }

        public async Task DeleteAsync(string termId, string actingUser)
        {
            var entity = await _context.Terminals.FirstOrDefaultAsync(t => t.term_id == termId)
                ?? throw new NotFoundException($"Terminal '{termId}' not found.");

            // Matches legacy TerminalDelete: remove linked init/key rows first, then the
            // terminal itself. No other PosBase entity references sw_terminal.
            var init = await _context.TerminalInits.FirstOrDefaultAsync(i => i.term_id == termId);
            if (init != null)
            {
                _context.TerminalInits.Remove(init);
                _audit.LogDelete(new { init.term_id }, "sw_term_init", actingUser);
            }

            var key = await _context.TerminalKeys.FirstOrDefaultAsync(k => k.term_id == termId);
            if (key != null)
            {
                _context.TerminalKeys.Remove(key);
                _audit.LogDelete(new { key.term_id }, "sw_term_key", actingUser);
            }

            _context.Terminals.Remove(entity);
            _audit.LogDelete(new { entity.term_id, entity.serial_number }, "sw_terminal", actingUser);

            await _context.SaveChangesAsync();
        }

        private async Task<Dictionary<string, string?>> GetMerchantNameMapAsync(IEnumerable<string?> merchantIds)
        {
            var ids = merchantIds.Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            if (ids.Count == 0) return new Dictionary<string, string?>();

            return await _context.Merchants.AsNoTracking()
                .Where(m => ids.Contains(m.merchant_id))
                .ToDictionaryAsync(m => m.merchant_id, m => m.name);
        }

        private async Task ValidateReferencesAsync(string merchantId, string? subMerchantId, string? storeId)
        {
            var merchantExists = await _context.Merchants.AsNoTracking().AnyAsync(m => m.merchant_id == merchantId);
            if (!merchantExists)
                throw new ValidationException($"Merchant '{merchantId}' does not exist.");

            if (!string.IsNullOrEmpty(subMerchantId))
            {
                var subMerchantExists = await _context.SubMerchants.AsNoTracking().AnyAsync(s => s.submerchant_id == subMerchantId);
                if (!subMerchantExists)
                    throw new ValidationException($"Sub merchant '{subMerchantId}' does not exist.");
            }

            if (!string.IsNullOrEmpty(storeId))
            {
                var storeExists = await _context.Stores.AsNoTracking().AnyAsync(s => s.store_id == storeId);
                if (!storeExists)
                    throw new ValidationException($"Store '{storeId}' does not exist.");
            }
        }

        private static void ApplyGeneralFields(SwTerminal e, string merchantId, string? subMerchantId, string? storeId,
            string? loketName, string? location, string? city, string? phone, bool active, string? groupName,
            string? subgroupName, string? criteria, string? nmid, string? mpan, string? chatId, string? brand,
            string? type, string serialNumber, bool security, string? ipAddress, string? macAddress, string? bankName,
            string? bankBranch, string? bankCode, string? bankAccNumber, string? bankAccName, string? virtualAccount,
            string? vaName)
        {
            e.merchant_id = merchantId;
            e.submerchant_id = subMerchantId;
            e.store_id = storeId;
            e.loket_name = loketName;
            e.location = location;
            e.city = city;
            e.phone = phone;
            e.status = active ? "1" : "0";
            e.group_name = groupName;
            e.subgroup_name = subgroupName;
            e.criteria = criteria;
            e.nmid = nmid;
            e.mpan = mpan;
            e.chat_id = chatId;
            e.brand = brand;
            e.type = type;
            e.serial_number = serialNumber;
            e.security = security ? "1" : "0";
            e.ip_address = ipAddress;
            e.mac_address = macAddress;
            e.bank_name = bankName;
            e.bank_branch = bankBranch;
            e.bank_code = bankCode;
            e.bank_acc_number = bankAccNumber;
            e.bank_acc_name = bankAccName;
            e.virtual_account = virtualAccount;
            e.va_name = vaName;
        }

        private static object ToAuditSnapshot(SwTerminal e) => new
        {
            e.merchant_id, e.submerchant_id, e.store_id, e.loket_name, e.location, e.city, e.phone,
            e.status, e.group_name, e.subgroup_name, e.criteria, e.nmid, e.mpan, e.chat_id, e.brand,
            e.type, e.serial_number, e.security, e.ip_address, e.mac_address, e.bank_name,
            e.bank_branch, e.bank_code, e.bank_acc_number, e.bank_acc_name, e.virtual_account, e.va_name
        };

        private static TerminalDto ToDto(SwTerminal e, SwTerminalKey? key, SwTerminalInit? init, Dictionary<string, string?> merchantNames)
        {
            merchantNames.TryGetValue(e.merchant_id ?? string.Empty, out var merchantName);

            return new TerminalDto(
                e.term_id, e.merchant_id, merchantName, e.submerchant_id, e.store_id, e.loket_name, e.location,
                e.city, e.phone, e.status == "1",
                e.group_name, e.subgroup_name, e.criteria, e.nmid, e.mpan, e.chat_id,
                e.brand, e.type, e.serial_number, init?.enable_init == "1", init?.init_id, e.inst_date,
                e.security == "1", e.ip_address, e.mac_address,
                e.bank_name, e.bank_branch, e.bank_code, e.bank_acc_number, e.bank_acc_name,
                e.virtual_account, e.va_name,
                key?.key_length, key?.pinblock_format, key?.master_key, key?.master_kcv,
                key?.key_under_lmk, key?.key_under_zmk, key?.key_check_value);
        }
    }
}
