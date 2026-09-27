using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.AccountTypes;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.AccountTypes
{
    public class AccountTypeService : IAccountTypeService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public AccountTypeService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<AccountTypeDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.AccountTypes.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(a => a.acct_type.ToLower().Contains(f) || (a.name ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(a => a.acct_type).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<AccountTypeDto?> GetByIdAsync(string acctType)
        {
            var entity = await _context.AccountTypes.AsNoTracking().FirstOrDefaultAsync(a => a.acct_type == acctType);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<AccountTypeDto> CreateAsync(CreateAccountTypeRequest request, string actingUser)
        {
            var exists = await _context.AccountTypes.AsNoTracking().AnyAsync(a => a.acct_type == request.AcctType);
            if (exists)
                throw new ConflictException($"Account type '{request.AcctType}' already exists.");

            var entity = new SwAccountType
            {
                acct_type = request.AcctType,
                name = request.Name,
                status = "1",
                created_by = actingUser,
                created_dt = DateTime.UtcNow
            };

            _context.AccountTypes.Add(entity);
            _audit.LogInsert(entity, "sw_account_types", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<AccountTypeDto> UpdateAsync(string acctType, UpdateAccountTypeRequest request, string actingUser)
        {
            var entity = await _context.AccountTypes.FirstOrDefaultAsync(a => a.acct_type == acctType)
                ?? throw new NotFoundException($"Account type '{acctType}' not found.");

            var before = new { entity.name, entity.status };
            entity.name = request.Name;
            entity.status = request.Active ? "1" : "0";
            entity.updated_by = actingUser;
            entity.updated_dt = DateTime.UtcNow;

            _audit.LogUpdate(before, new { entity.name, entity.status }, "sw_account_types", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(string acctType, string actingUser)
        {
            var entity = await _context.AccountTypes.FirstOrDefaultAsync(a => a.acct_type == acctType)
                ?? throw new NotFoundException($"Account type '{acctType}' not found.");

            _context.AccountTypes.Remove(entity);
            _audit.LogDelete(new { entity.acct_type }, "sw_account_types", actingUser);

            await _context.SaveChangesAsync();
        }

        private static AccountTypeDto ToDto(SwAccountType e) => new(e.acct_type, e.name, e.status == "1");
    }
}
