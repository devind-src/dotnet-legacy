using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.CardAccounts;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.CardAccounts
{
    public class CardAccountService : ICardAccountService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public CardAccountService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<CardAccountDto>> GetRecordsAsync(string? filter = null)
        {
            var query = from a in _context.CardAccounts.AsNoTracking()
                        join g in _context.CardGroups.AsNoTracking() on a.group_id equals g.group_id into gj
                        from g in gj.DefaultIfEmpty()
                        join t in _context.AccountTypes.AsNoTracking() on a.acc_type equals t.acct_type into tj
                        from t in tj.DefaultIfEmpty()
                        select new { Account = a, GroupName = g == null ? null : g.group_name, AccTypeName = t == null ? null : t.name };

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(x => (x.GroupName ?? "").ToLower().Contains(f) || (x.AccTypeName ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(x => x.Account.id).ToListAsync();
            return list.Select(x => ToDto(x.Account, x.GroupName, x.AccTypeName)).ToList();
        }

        public async Task<CardAccountDto?> GetByIdAsync(int id)
        {
            var entity = await _context.CardAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.id == id);
            if (entity == null) return null;

            var groupName = await _context.CardGroups.AsNoTracking()
                .Where(g => g.group_id == entity.group_id).Select(g => g.group_name).FirstOrDefaultAsync();
            var accTypeName = await _context.AccountTypes.AsNoTracking()
                .Where(t => t.acct_type == entity.acc_type).Select(t => t.name).FirstOrDefaultAsync();

            return ToDto(entity, groupName, accTypeName);
        }

        public async Task<CardAccountDto> CreateAsync(CreateCardAccountRequest request, string actingUser)
        {
            if (!await _context.CardGroups.AsNoTracking().AnyAsync(g => g.group_id == request.GroupId))
                throw new ValidationException($"Card group '{request.GroupId}' does not exist.");

            var entity = new SwAccount
            {
                group_id = request.GroupId,
                acc_type = request.AccType,
                pan_verification = request.PanVerification ? "1" : "0",
                pin_verification = request.PinVerification ? "1" : "0"
            };

            _context.CardAccounts.Add(entity);
            _audit.LogInsert(entity, "sw_accounts", actingUser);

            await _context.SaveChangesAsync();
            return (await GetByIdAsync(entity.id))!;
        }

        public async Task<CardAccountDto> UpdateAsync(int id, UpdateCardAccountRequest request, string actingUser)
        {
            var entity = await _context.CardAccounts.FirstOrDefaultAsync(a => a.id == id)
                ?? throw new NotFoundException($"Account '{id}' not found.");

            var before = new { entity.acc_type, entity.pan_verification, entity.pin_verification };
            entity.acc_type = request.AccType;
            entity.pan_verification = request.PanVerification ? "1" : "0";
            entity.pin_verification = request.PinVerification ? "1" : "0";

            _audit.LogUpdate(before, new { entity.acc_type, entity.pan_verification, entity.pin_verification }, "sw_accounts", actingUser);

            await _context.SaveChangesAsync();
            return (await GetByIdAsync(entity.id))!;
        }

        public async Task DeleteAsync(int id, string actingUser)
        {
            var entity = await _context.CardAccounts.FirstOrDefaultAsync(a => a.id == id)
                ?? throw new NotFoundException($"Account '{id}' not found.");

            _context.CardAccounts.Remove(entity);
            _audit.LogDelete(new { entity.id, entity.group_id }, "sw_accounts", actingUser);

            await _context.SaveChangesAsync();
        }

        private static CardAccountDto ToDto(SwAccount e, string? groupName, string? accTypeName) => new(
            e.id, e.group_id, groupName, e.acc_type, accTypeName, e.pan_verification == "1", e.pin_verification == "1");
    }
}
