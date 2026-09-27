using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.CashoutMiniAtms;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.CashoutMiniAtms
{
    public class CashoutMiniAtmService : ICashoutMiniAtmService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public CashoutMiniAtmService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        // Mirrors legacy CashoutBankGetRecords filter fields exactly (group_name, bank_name,
        // bank_code, acc_name — note acc_number is intentionally NOT filtered on, unlike
        // CashoutWithdrawal's filter).
        public async Task<IReadOnlyList<CashoutMiniAtmDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.CashoutMiniAtms.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(x => (x.group_name ?? "").ToLower().Contains(f)
                    || (x.bank_name ?? "").ToLower().Contains(f)
                    || (x.bank_code ?? "").ToLower().Contains(f)
                    || (x.acc_name ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(x => x.id).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<CashoutMiniAtmDto?> GetByIdAsync(long id)
        {
            var entity = await _context.CashoutMiniAtms.AsNoTracking().FirstOrDefaultAsync(x => x.id == id);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<CashoutMiniAtmDto> CreateAsync(CreateCashoutMiniAtmRequest request, string actingUser)
        {
            await ValidateGroupNameAsync(request.GroupName);

            if (await _context.CashoutMiniAtms.AsNoTracking()
                    .AnyAsync(x => x.group_name == request.GroupName && x.bank_code == request.BankCode && x.acc_number == request.AccNumber))
                throw new ConflictException("Kombinasi group name, bank code dan account number sudah terdaftar.");

            // id is GENERATED ALWAYS AS IDENTITY — do not assign manually (see entity note).
            var entity = new SwCashoutBank
            {
                group_name = request.GroupName,
                bank_name = request.BankName,
                bank_code = request.BankCode,
                acc_number = request.AccNumber,
                acc_name = request.AccName
            };

            _context.CashoutMiniAtms.Add(entity);
            _audit.LogInsert(entity, "sw_cashout_bank", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<CashoutMiniAtmDto> UpdateAsync(long id, UpdateCashoutMiniAtmRequest request, string actingUser)
        {
            var entity = await _context.CashoutMiniAtms.FirstOrDefaultAsync(x => x.id == id)
                ?? throw new NotFoundException($"Cashout Fee Mini ATM account '{id}' not found.");

            await ValidateGroupNameAsync(request.GroupName);

            var before = ToDto(entity);

            entity.group_name = request.GroupName;
            entity.bank_name = request.BankName;
            entity.bank_code = request.BankCode;
            entity.acc_number = request.AccNumber;
            entity.acc_name = request.AccName;

            _audit.LogUpdate(before, ToDto(entity), "sw_cashout_bank", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(long id, string actingUser)
        {
            var entity = await _context.CashoutMiniAtms.FirstOrDefaultAsync(x => x.id == id)
                ?? throw new NotFoundException($"Cashout Fee Mini ATM account '{id}' not found.");

            _context.CashoutMiniAtms.Remove(entity);
            _audit.LogDelete(entity, "sw_cashout_bank", actingUser);

            await _context.SaveChangesAsync();
        }

        // Mirrors legacy IsGroupNameExist() — group must exist as a Merchant Group OR a
        // SubMerchant Group, checked on Create AND Update (no FK constraint exists in
        // Postgres).
        private async Task ValidateGroupNameAsync(string groupName)
        {
            var existsAsMerchantGroup = await _context.MerchantGroups.AsNoTracking().AnyAsync(g => g.group_name == groupName);
            if (existsAsMerchantGroup) return;

            var existsAsSubMerchantGroup = await _context.SubMerchantGroups.AsNoTracking().AnyAsync(g => g.group_name == groupName);
            if (existsAsSubMerchantGroup) return;

            throw new ValidationException($"Group name '{groupName}' does not exist.");
        }

        private static CashoutMiniAtmDto ToDto(SwCashoutBank e)
            => new(e.id, e.group_name, e.bank_name, e.bank_code, e.acc_number, e.acc_name);
    }
}
