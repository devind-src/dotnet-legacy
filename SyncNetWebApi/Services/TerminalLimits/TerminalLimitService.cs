using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.TerminalLimits;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.TerminalLimits
{
    public class TerminalLimitService : ITerminalLimitService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public TerminalLimitService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<TerminalLimitDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.TerminalLimits.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(l => (l.limit_name ?? "").ToLower().Contains(f) || (l.group_name ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(l => l.id).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<TerminalLimitDto?> GetByIdAsync(long id)
        {
            var entity = await _context.TerminalLimits.AsNoTracking().FirstOrDefaultAsync(l => l.id == id);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<TerminalLimitDto> CreateAsync(CreateTerminalLimitRequest request, string actingUser)
        {
            ValidateAmounts(request.MinWithdrawal, request.MaxWithdrawal, request.MinTransfer, request.MaxTransfer,
                request.MinPurchase, request.MaxPurchase, request.MinPayment, request.MaxPayment);

            var duplicate = await _context.TerminalLimits.AsNoTracking()
                .AnyAsync(l => l.limit_name == request.LimitName && l.group_name == request.GroupName);
            if (duplicate)
                throw new ConflictException($"Combination limit name '{request.LimitName}' & group name already exists.");

            var entity = new SwTerminalLimit
            {
                limit_name = request.LimitName,
                group_name = request.GroupName,
                min_withdrawal = request.MinWithdrawal,
                max_withdrawal = request.MaxWithdrawal,
                min_transfer = request.MinTransfer,
                max_transfer = request.MaxTransfer,
                min_purchase = request.MinPurchase,
                max_purchase = request.MaxPurchase,
                min_payment = request.MinPayment,
                max_payment = request.MaxPayment
            };

            _context.TerminalLimits.Add(entity);
            _audit.LogInsert(entity, "sw_term_limit", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<TerminalLimitDto> UpdateAsync(long id, UpdateTerminalLimitRequest request, string actingUser)
        {
            var entity = await _context.TerminalLimits.FirstOrDefaultAsync(l => l.id == id)
                ?? throw new NotFoundException($"Limit '{id}' not found.");

            ValidateAmounts(request.MinWithdrawal, request.MaxWithdrawal, request.MinTransfer, request.MaxTransfer,
                request.MinPurchase, request.MaxPurchase, request.MinPayment, request.MaxPayment);

            var before = ToAuditSnapshot(entity);

            entity.group_name = request.GroupName;
            entity.min_withdrawal = request.MinWithdrawal;
            entity.max_withdrawal = request.MaxWithdrawal;
            entity.min_transfer = request.MinTransfer;
            entity.max_transfer = request.MaxTransfer;
            entity.min_purchase = request.MinPurchase;
            entity.max_purchase = request.MaxPurchase;
            entity.min_payment = request.MinPayment;
            entity.max_payment = request.MaxPayment;

            _audit.LogUpdate(before, ToAuditSnapshot(entity), "sw_term_limit", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(long id, string actingUser)
        {
            var entity = await _context.TerminalLimits.FirstOrDefaultAsync(l => l.id == id)
                ?? throw new NotFoundException($"Limit '{id}' not found.");

            _context.TerminalLimits.Remove(entity);
            _audit.LogDelete(new { entity.id, entity.limit_name }, "sw_term_limit", actingUser);

            await _context.SaveChangesAsync();
        }

        /// <summary>Mirrors legacy Terminal Limit Detail.razor IsValid(): withdrawal min/max
        /// must be multiples of 1000, and every min must not exceed its matching max.</summary>
        private static void ValidateAmounts(long minW, long maxW, long minT, long maxT, long minP, long maxP, long minPay, long maxPay)
        {
            if (minW % 1000 != 0)
                throw new ValidationException("Invalid min withdrawal amount, must be a multiple of 1000.");
            if (maxW % 1000 != 0)
                throw new ValidationException("Invalid max withdrawal amount, must be a multiple of 1000.");
            if (minW > maxW)
                throw new ValidationException("Min withdrawal amount cannot be greater than max withdrawal amount.");
            if (minT > maxT)
                throw new ValidationException("Min transfer amount cannot be greater than max transfer amount.");
            if (minP > maxP)
                throw new ValidationException("Min purchase amount cannot be greater than max purchase amount.");
            if (minPay > maxPay)
                throw new ValidationException("Min payment amount cannot be greater than max payment amount.");
        }

        private static object ToAuditSnapshot(SwTerminalLimit e) => new
        {
            e.group_name, e.min_withdrawal, e.max_withdrawal, e.min_transfer, e.max_transfer,
            e.min_purchase, e.max_purchase, e.min_payment, e.max_payment
        };

        private static TerminalLimitDto ToDto(SwTerminalLimit e) => new(
            e.id, e.limit_name ?? string.Empty, e.group_name, e.min_withdrawal, e.max_withdrawal,
            e.min_transfer, e.max_transfer, e.min_purchase, e.max_purchase, e.min_payment, e.max_payment);
    }
}
