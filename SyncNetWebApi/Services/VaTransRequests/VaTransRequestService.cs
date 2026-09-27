using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.VaTransRequests;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.VaTransRequests
{
    /// <summary>Backs Virtual Account &gt; Topup, Adjustment and Approval — three legacy pages
    /// over one table (va_trans_request). Approval additionally mutates va_account.balance and
    /// inserts a row into sw_trans_pg (the core switch ledger, shared with the live switching
    /// engine) — see ApproveAsync for the full replicated-from-legacy flow and the fee_bil/
    /// fee_iss column-name bug fix.</summary>
    public class VaTransRequestService : IVaTransRequestService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public VaTransRequestService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public Task<IReadOnlyList<VaTransRequestDto>> GetTopupRecordsAsync(string? filter = null)
            => GetRecordsAsync(filter, q => q.Where(x => x.tran_type == VaTranType.Topup));

        public Task<IReadOnlyList<VaTransRequestDto>> GetAdjustmentRecordsAsync(string? filter = null)
            => GetRecordsAsync(filter, q => q.Where(x => x.tran_type == VaTranType.AdjCredit || x.tran_type == VaTranType.AdjDebet));

        public Task<IReadOnlyList<VaTransRequestDto>> GetApprovalRecordsAsync(string? filter = null)
            => GetRecordsAsync(filter, q => q);

        private async Task<IReadOnlyList<VaTransRequestDto>> GetRecordsAsync(
            string? filter, Func<IQueryable<VaTransRequest>, IQueryable<VaTransRequest>> scope)
        {
            var query = scope(_context.VaTransRequests.AsNoTracking());

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(x =>
                    (x.acc_nr ?? "").ToLower().Contains(f) ||
                    (x.trace_nr ?? "").ToLower().Contains(f) ||
                    (x.reff_nr ?? "").ToLower().Contains(f) ||
                    (x.description ?? "").ToLower().Contains(f) ||
                    (x.usr_name ?? "").ToLower().Contains(f) ||
                    (x.spv_name ?? "").ToLower().Contains(f));
            }

            var rows = await (from x in query
                               join a in _context.VaAccounts.AsNoTracking() on x.acc_nr equals a.acc_nr into aj
                               from a in aj.DefaultIfEmpty()
                               orderby x.tran_nr descending
                               select new { Request = x, VaName = a == null ? null : a.va_name, GroupName = a == null ? null : a.group_name })
                              .ToListAsync();

            return rows.Select(r => ToDto(r.Request, r.VaName, r.GroupName)).ToList();
        }

        public async Task<VaTransRequestDto?> GetByIdAsync(int tranNr)
        {
            var entity = await _context.VaTransRequests.AsNoTracking().FirstOrDefaultAsync(x => x.tran_nr == tranNr);
            if (entity == null) return null;

            var account = await _context.VaAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.acc_nr == entity.acc_nr);
            return ToDto(entity, account?.va_name, account?.group_name);
        }

        public async Task<VaTransRequestDto> CreateTopupAsync(CreateVaTopupRequest request, string actingUser)
        {
            var account = await _context.VaAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.acc_nr == request.AccNr)
                ?? throw new ValidationException($"VA account '{request.AccNr}' does not exist.");

            var entity = new VaTransRequest
            {
                acc_nr = request.AccNr,
                tran_type = VaTranType.Topup,
                debet_credit = "C",
                trace_nr = GenerateDigits(12),
                reff_nr = "TC" + GenerateDigits(10),
                description = request.Description,
                amount = request.Amount,
                balance = 0,
                usr_name = actingUser,
                request_date = DateTime.Now,
                status = VaTranStatus.Request
            };

            _context.VaTransRequests.Add(entity);
            _audit.LogInsert(entity, "va_trans_request", actingUser);
            await _context.SaveChangesAsync();

            return ToDto(entity, account.va_name, account.group_name);
        }

        public async Task<VaTransRequestDto> CreateAdjustmentAsync(CreateVaAdjustmentRequest request, string actingUser)
        {
            var account = await _context.VaAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.acc_nr == request.AccNr)
                ?? throw new ValidationException($"VA account '{request.AccNr}' does not exist.");

            var entity = new VaTransRequest
            {
                acc_nr = request.AccNr,
                tran_type = request.DebetCredit == "D" ? VaTranType.AdjDebet : VaTranType.AdjCredit,
                debet_credit = request.DebetCredit,
                trace_nr = GenerateDigits(12),
                reff_nr = $"A{request.DebetCredit}{GenerateDigits(10)}",
                description = request.Description,
                amount = request.Amount,
                balance = 0,
                usr_name = actingUser,
                request_date = DateTime.Now,
                status = VaTranStatus.Request
            };

            _context.VaTransRequests.Add(entity);
            _audit.LogInsert(entity, "va_trans_request", actingUser);
            await _context.SaveChangesAsync();

            return ToDto(entity, account.va_name, account.group_name);
        }

        public async Task<VaTransRequestDto> ApproveAsync(int tranNr, string actingUser)
        {
            var entity = await _context.VaTransRequests.FirstOrDefaultAsync(x => x.tran_nr == tranNr)
                ?? throw new NotFoundException($"VA transaction request '{tranNr}' not found.");

            if (entity.status != VaTranStatus.Request)
                throw new ValidationException("This request has already been processed.");

            var account = await _context.VaAccounts.FirstOrDefaultAsync(a => a.acc_nr == entity.acc_nr)
                ?? throw new NotFoundException($"VA account '{entity.acc_nr}' not found.");

            var amount = entity.amount ?? 0;
            var balance = account.balance ?? 0;
            var newBalance = entity.debet_credit == "D" ? balance - amount : balance + amount;

            if (entity.debet_credit == "D" && newBalance < 0)
                throw new ValidationException("Insufficient fund");

            var dtNow = DateTime.Now;

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var before = new { entity.approval_date, entity.spv_name, entity.status };

            account.balance = newBalance;

            entity.approval_date = dtNow;
            entity.spv_name = actingUser;
            entity.status = VaTranStatus.Approved;
            // NOTE: legacy computes NewData.balance = newbalance in memory but its raw UPDATE
            // only writes approval_date/spv_name/status — va_trans_request.balance is never
            // actually persisted (verified against real approved rows, always 0). Replicated
            // as a dead-write: entity.balance is intentionally left untouched here.

            // Mirrors legacy VaTranRequestApproval — writes one row into the core switch ledger
            // (sw_trans_pg), the same table Queries > Transaction reads and the live switching
            // engine shares. Column names fee_bil/fee_iss fixed vs. legacy's raw SQL, which used
            // the non-existent fee_biller/fee_issuer (confirmed live, user-authorized fix).
            var swTranType = entity.debet_credit == "D" ? SwTranType.VaAdjustDebet : SwTranType.VaTopupCredit;
            var tranDatetime = dtNow.ToString("yyyyMMddHHmmss");

            var ledgerRow = new SwTransPg
            {
                switch_key = $"{swTranType}{tranDatetime}{entity.trace_nr}",
                source_node = "Dashboard",
                dest_node = "VirtualAccount",
                state = 1,
                tran_type = swTranType,
                tran_type_ext = entity.tran_type,
                amount_tran_req = amount,
                amount_tran_rsp = amount,
                amount_va = amount,
                fee_total = 0,
                fee_acq = 0,
                fee_swt = 0,
                fee_bil = 0,
                fee_iss = 0,
                tran_datetime = tranDatetime,
                trace_number = entity.trace_nr,
                reff_number = entity.reff_nr,
                merchant_type = "6010",
                last_balance = newBalance,
                receiving_inst_id = "VA",
                from_acc_number = entity.acc_nr,
                to_acc_number = entity.acc_nr,
                va_acc_number = entity.acc_nr,
                source_tran = "1", // SourceTran.EXTERNAL
                auth_tran = "0", // AuthTran.INTERNAL
                node_data_req = "",
                node_data_rsp = "",
                resp_code_rsp = "00",
                tran_reversed = "0",
                time_req = dtNow,
                time_rsp = dtNow
            };
            _context.SwTransactions.Add(ledgerRow);

            _audit.LogUpdate(before, new { entity.approval_date, entity.spv_name, entity.status }, "va_trans_request", actingUser);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ToDto(entity, account.va_name, account.group_name);
        }

        public async Task<VaTransRequestDto> RejectAsync(int tranNr, string actingUser)
        {
            var entity = await _context.VaTransRequests.FirstOrDefaultAsync(x => x.tran_nr == tranNr)
                ?? throw new NotFoundException($"VA transaction request '{tranNr}' not found.");

            if (entity.status != VaTranStatus.Request)
                throw new ValidationException("This request has already been processed.");

            var before = new { entity.status };

            // Mirrors legacy VaTranRequestReject exactly — only status changes. spv_name/
            // approval_date are NOT set on reject (legacy gap: only VaTranRequestApproval sets
            // them before saving). Who rejected it is still captured in sw_audit via actingUser.
            entity.status = VaTranStatus.Rejected;

            _audit.LogUpdate(before, new { entity.status }, "va_trans_request", actingUser);
            await _context.SaveChangesAsync();

            var account = await _context.VaAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.acc_nr == entity.acc_nr);
            return ToDto(entity, account?.va_name, account?.group_name);
        }

        private static string GenerateDigits(int length)
        {
            Span<byte> buffer = stackalloc byte[length];
            RandomNumberGenerator.Fill(buffer);

            var chars = new char[length];
            for (var i = 0; i < length; i++)
                chars[i] = (char)('0' + buffer[i] % 10);

            return new string(chars);
        }

        private static VaTransRequestDto ToDto(VaTransRequest e, string? vaName, string? groupName) => new(
            e.tran_nr, e.acc_nr, vaName, groupName, e.amount ?? 0, e.tran_type, VaTranType.GetName(e.tran_type),
            e.trace_nr, e.debet_credit, e.reff_nr, e.description, e.request_date, e.approval_date,
            e.usr_name, e.spv_name, e.status, VaTranStatus.GetName(e.status));
    }
}
