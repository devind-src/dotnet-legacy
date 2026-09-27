using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.CashoutWithdrawals;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.CashoutWithdrawals
{
    public class CashoutWithdrawalService : ICashoutWithdrawalService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public CashoutWithdrawalService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        // Mirrors legacy TerminalBankGetRecords filter fields exactly (terminal_id, bank_name,
        // bank_code, acc_number, acc_name — note this set differs from CashoutMiniAtm's, which
        // does not filter on acc_number).
        public async Task<IReadOnlyList<CashoutWithdrawalDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.CashoutWithdrawals.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(x => (x.terminal_id ?? "").ToLower().Contains(f)
                    || (x.bank_name ?? "").ToLower().Contains(f)
                    || (x.bank_code ?? "").ToLower().Contains(f)
                    || (x.acc_number ?? "").ToLower().Contains(f)
                    || (x.acc_name ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(x => x.id).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<CashoutWithdrawalDto?> GetByIdAsync(long id)
        {
            var entity = await _context.CashoutWithdrawals.AsNoTracking().FirstOrDefaultAsync(x => x.id == id);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<CashoutWithdrawalDto> CreateAsync(CreateCashoutWithdrawalRequest request, string actingUser)
        {
            await ValidateReferencesAsync(request.TerminalId, request.MerchantId);

            if (await _context.CashoutWithdrawals.AsNoTracking()
                    .AnyAsync(x => x.terminal_id == request.TerminalId && x.bank_code == request.BankCode && x.acc_number == request.AccNumber))
                throw new ConflictException("Kombinasi terminal, bank code dan account number sudah terdaftar.");

            // id is GENERATED ALWAYS AS IDENTITY — do not assign manually (see entity note).
            var entity = new SwTerminalBank
            {
                terminal_id = request.TerminalId,
                merchant_id = request.MerchantId,
                bank_name = request.BankName,
                bank_code = request.BankCode,
                acc_number = request.AccNumber,
                acc_name = request.AccName
            };

            _context.CashoutWithdrawals.Add(entity);
            _audit.LogInsert(entity, "sw_terminal_bank", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<CashoutWithdrawalDto> UpdateAsync(long id, UpdateCashoutWithdrawalRequest request, string actingUser)
        {
            var entity = await _context.CashoutWithdrawals.FirstOrDefaultAsync(x => x.id == id)
                ?? throw new NotFoundException($"Cashout withdrawal account '{id}' not found.");

            await ValidateReferencesAsync(request.TerminalId, request.MerchantId);

            var before = ToDto(entity);

            entity.terminal_id = request.TerminalId;
            entity.merchant_id = request.MerchantId;
            entity.bank_name = request.BankName;
            entity.bank_code = request.BankCode;
            entity.acc_number = request.AccNumber;
            entity.acc_name = request.AccName;

            _audit.LogUpdate(before, ToDto(entity), "sw_terminal_bank", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(long id, string actingUser)
        {
            var entity = await _context.CashoutWithdrawals.FirstOrDefaultAsync(x => x.id == id)
                ?? throw new NotFoundException($"Cashout withdrawal account '{id}' not found.");

            _context.CashoutWithdrawals.Remove(entity);
            _audit.LogDelete(entity, "sw_terminal_bank", actingUser);

            await _context.SaveChangesAsync();
        }

        // Mirrors legacy IsValid()/IsTerminalExist() — terminal and merchant must both exist,
        // checked on Create AND Update (no FK constraint exists in Postgres for either).
        private async Task ValidateReferencesAsync(string terminalId, string merchantId)
        {
            if (!await _context.Terminals.AsNoTracking().AnyAsync(t => t.term_id == terminalId))
                throw new ValidationException($"Terminal '{terminalId}' does not exist.");

            if (!await _context.Merchants.AsNoTracking().AnyAsync(m => m.merchant_id == merchantId))
                throw new ValidationException($"Merchant '{merchantId}' does not exist.");
        }

        private static CashoutWithdrawalDto ToDto(SwTerminalBank e)
            => new(e.id, e.terminal_id, e.merchant_id, e.bank_name, e.bank_code, e.acc_number, e.acc_name);
    }
}
