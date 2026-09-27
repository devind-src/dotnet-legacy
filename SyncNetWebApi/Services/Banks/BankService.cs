using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.Banks;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.Banks
{
    public class BankService : IBankService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public BankService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<BankDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.Banks.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(b => (b.bank ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(b => b.bank).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<BankDto?> GetByIdAsync(int id)
        {
            var entity = await _context.Banks.AsNoTracking().FirstOrDefaultAsync(b => b.id == id);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<BankDto> CreateAsync(CreateBankRequest request, string actingUser)
        {
            var entity = new SwBank
            {
                bank = request.Bank,
                cbc = request.Cbc,
                status = "1",
                created_by = actingUser,
                created_dt = DateTime.UtcNow
            };

            _context.Banks.Add(entity);
            _audit.LogInsert(entity, "sw_bank", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<BankDto> UpdateAsync(int id, UpdateBankRequest request, string actingUser)
        {
            var entity = await _context.Banks.FirstOrDefaultAsync(b => b.id == id)
                ?? throw new NotFoundException($"Bank '{id}' not found.");

            var before = new { entity.bank, entity.cbc, entity.status };
            entity.bank = request.Bank;
            entity.cbc = request.Cbc;
            entity.status = request.Active ? "1" : "0";
            entity.updated_by = actingUser;
            entity.updated_dt = DateTime.UtcNow;

            _audit.LogUpdate(before, new { entity.bank, entity.cbc, entity.status }, "sw_bank", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(int id, string actingUser)
        {
            var entity = await _context.Banks.FirstOrDefaultAsync(b => b.id == id)
                ?? throw new NotFoundException($"Bank '{id}' not found.");

            _context.Banks.Remove(entity);
            _audit.LogDelete(new { entity.id, entity.bank }, "sw_bank", actingUser);

            await _context.SaveChangesAsync();
        }

        private static BankDto ToDto(SwBank e) => new(e.id, e.bank, e.cbc, e.status == "1");
    }
}
