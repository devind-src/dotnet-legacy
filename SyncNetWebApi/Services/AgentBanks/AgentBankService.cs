using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.AgentBanks;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.AgentBanks
{
    public class AgentBankService : IAgentBankService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public AgentBankService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        // Mirrors legacy AgentBankGetRecords filter fields exactly (nmid, bank_name, bank_code,
        // acc_name — acc_number is intentionally NOT filtered on, same pattern as Cashout >
        // Fee Mini ATM).
        public async Task<IReadOnlyList<AgentBankDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.AgentBanks.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(x => (x.nmid ?? "").ToLower().Contains(f)
                    || (x.bank_name ?? "").ToLower().Contains(f)
                    || (x.bank_code ?? "").ToLower().Contains(f)
                    || (x.acc_name ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(x => x.id).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<AgentBankDto?> GetByIdAsync(long id)
        {
            var entity = await _context.AgentBanks.AsNoTracking().FirstOrDefaultAsync(x => x.id == id);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<AgentBankDto> CreateAsync(CreateAgentBankRequest request, string actingUser)
        {
            if (await _context.AgentBanks.AsNoTracking()
                    .AnyAsync(x => x.nmid == request.Nmid && x.bank_code == request.BankCode && x.acc_number == request.AccNumber))
                throw new ConflictException("Kombinasi NMID, bank code dan account number sudah terdaftar.");

            // id is GENERATED ALWAYS AS IDENTITY — do not assign manually (see entity note).
            var entity = new SwAgentBank
            {
                nmid = request.Nmid,
                bank_name = request.BankName,
                bank_code = request.BankCode,
                acc_number = request.AccNumber,
                acc_name = request.AccName
            };

            _context.AgentBanks.Add(entity);
            _audit.LogInsert(entity, "sw_agent_bank", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<AgentBankDto> UpdateAsync(long id, UpdateAgentBankRequest request, string actingUser)
        {
            var entity = await _context.AgentBanks.FirstOrDefaultAsync(x => x.id == id)
                ?? throw new NotFoundException($"Agent bank '{id}' not found.");

            var before = ToDto(entity);

            entity.nmid = request.Nmid;
            entity.bank_name = request.BankName;
            entity.bank_code = request.BankCode;
            entity.acc_number = request.AccNumber;
            entity.acc_name = request.AccName;

            _audit.LogUpdate(before, ToDto(entity), "sw_agent_bank", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(long id, string actingUser)
        {
            var entity = await _context.AgentBanks.FirstOrDefaultAsync(x => x.id == id)
                ?? throw new NotFoundException($"Agent bank '{id}' not found.");

            _context.AgentBanks.Remove(entity);
            _audit.LogDelete(entity, "sw_agent_bank", actingUser);

            await _context.SaveChangesAsync();
        }

        private static AgentBankDto ToDto(SwAgentBank e)
            => new(e.id, e.nmid, e.bank_name, e.bank_code, e.acc_number, e.acc_name);
    }
}
