using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.VaAccounts;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.VaAccounts
{
    public class VaAccountService : IVaAccountService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public VaAccountService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<VaAccountDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.VaAccounts.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(a =>
                    a.acc_nr.ToLower().Contains(f) ||
                    (a.group_name ?? "").ToLower().Contains(f) ||
                    (a.va_notes ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(a => a.acc_nr).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<VaAccountDto?> GetByIdAsync(string accNr)
        {
            var entity = await _context.VaAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.acc_nr == accNr);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<VaAccountDto> CreateAsync(CreateVaAccountRequest request, string actingUser)
        {
            if (await _context.VaAccounts.AsNoTracking().AnyAsync(a => a.acc_nr == request.AccNr))
                throw new ConflictException($"VA account '{request.AccNr}' already exists.");

            if (!await _context.VaGroups.AsNoTracking().AnyAsync(g => g.group_name == request.GroupName))
                throw new ValidationException($"VA group '{request.GroupName}' does not exist.");

            if (await _context.VaAccounts.AsNoTracking().AnyAsync(a => a.va_name == request.VaName))
                throw new ConflictException($"VA name '{request.VaName}' already exists, please choose another name.");

            var entity = new VaAccount
            {
                acc_nr = request.AccNr,
                group_name = request.GroupName,
                va_name = request.VaName,
                va_notes = request.VaNotes,
                status = request.Active ? "1" : "0",
                low_balance = request.LowBalance,
                min_balance = request.MinBalance,
                balance = 0
            };

            _context.VaAccounts.Add(entity);
            _audit.LogInsert(entity, "va_account", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<VaAccountDto> UpdateAsync(string accNr, UpdateVaAccountRequest request, string actingUser)
        {
            var entity = await _context.VaAccounts.FirstOrDefaultAsync(a => a.acc_nr == accNr)
                ?? throw new NotFoundException($"VA account '{accNr}' not found.");

            // Mirrors legacy's raw UPDATE — only va_notes/low_balance/min_balance/status are
            // ever written by this form. group_name/va_name/balance are never touched here.
            var before = new { entity.va_notes, entity.low_balance, entity.min_balance, entity.status };

            entity.va_notes = request.VaNotes;
            entity.low_balance = request.LowBalance;
            entity.min_balance = request.MinBalance;
            entity.status = request.Active ? "1" : "0";

            _audit.LogUpdate(before, new { entity.va_notes, entity.low_balance, entity.min_balance, entity.status }, "va_account", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(string accNr, string actingUser)
        {
            var entity = await _context.VaAccounts.FirstOrDefaultAsync(a => a.acc_nr == accNr)
                ?? throw new NotFoundException($"VA account '{accNr}' not found.");

            _context.VaAccounts.Remove(entity);
            _audit.LogDelete(entity, "va_account", actingUser);

            await _context.SaveChangesAsync();
        }

        private static VaAccountDto ToDto(VaAccount e) => new(
            e.acc_nr, e.group_name, e.va_name, e.va_notes, e.status == "1",
            e.low_balance ?? 0, e.min_balance ?? 0, e.balance ?? 0);
    }
}
