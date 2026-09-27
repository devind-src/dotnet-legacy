using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.Common;
using SyncNetApi.Dtos.Queries;
using SyncNetApi.Entities;

namespace SyncNetApi.Services.Queries
{
    /// <summary>Read-only over sw_trans_pg — the core switch ledger, shared with the live
    /// switching engine and with Virtual Account (see SwTransPg's own doc comment). This
    /// service never writes to it. Mirrors legacy DbSwitchNetService.TransGetRecords/
    /// TransGetRow/TransGetReversal (Queries &gt; Transaction).</summary>
    public class QueryTransactionService : IQueryTransactionService
    {
        private readonly SyncNetDbContext _context;

        public QueryTransactionService(SyncNetDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResultDto<TransactionListItemDto>> SearchAsync(TransactionSearchRequest request)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize is < 1 or > 100 ? 25 : request.PageSize;

            var query = _context.SwTransactions.AsNoTracking().AsQueryable();

            // time_req is "timestamp without time zone" — a DateTime bound from the query
            // string can arrive with Kind=Utc/Local depending on what the client sent, and
            // Npgsql rejects writing/comparing a Kind-tagged value against a "without time
            // zone" column (see SwTransPg's doc comment / VA Statement §7.21). Strip it here
            // regardless of what the client sends, rather than trusting the frontend alone.
            var dateStart = request.DateStart.HasValue ? DateTime.SpecifyKind(request.DateStart.Value, DateTimeKind.Unspecified) : (DateTime?)null;
            var dateEnd = request.DateEnd.HasValue ? DateTime.SpecifyKind(request.DateEnd.Value, DateTimeKind.Unspecified) : (DateTime?)null;

            if (dateStart.HasValue) query = query.Where(x => x.time_req >= dateStart);
            if (dateEnd.HasValue) query = query.Where(x => x.time_req <= dateEnd);
            if (!string.IsNullOrWhiteSpace(request.SourceNode)) query = query.Where(x => x.source_node == request.SourceNode);
            if (!string.IsNullOrWhiteSpace(request.DestNode)) query = query.Where(x => x.dest_node == request.DestNode);
            if (!string.IsNullOrWhiteSpace(request.MerchantId)) query = query.Where(x => x.merchant_id == request.MerchantId);
            if (!string.IsNullOrWhiteSpace(request.TerminalId)) query = query.Where(x => x.terminal_id == request.TerminalId);
            if (!string.IsNullOrWhiteSpace(request.AccountNo)) query = query.Where(x => x.to_acc_number == request.AccountNo);
            if (!string.IsNullOrWhiteSpace(request.RespCode)) query = query.Where(x => x.resp_code_rsp == request.RespCode);
            if (!string.IsNullOrWhiteSpace(request.TraceNumber)) query = query.Where(x => x.trace_number == request.TraceNumber);
            if (!string.IsNullOrWhiteSpace(request.RefNumber)) query = query.Where(x => x.reff_number == request.RefNumber);

            query = query.OrderByDescending(x => x.tran_nr);

            // Always recomputed (no server-side session to skip it on later pages like legacy
            // does) — sw_trans_pg is date-range filtered by the frontend default (today), which
            // keeps COUNT cheap in practice.
            var totalRecords = await query.LongCountAsync();

            var rows = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    x.tran_nr,
                    x.time_req,
                    x.source_node,
                    x.dest_node,
                    x.pan,
                    x.tran_type,
                    x.tran_type_ext,
                    x.amount_tran_req,
                    x.receiving_inst_id,
                    x.to_acc_number,
                    x.merchant_id,
                    x.terminal_id,
                    x.resp_code_rsp
                })
                .ToListAsync();

            var items = rows.Select(x => new TransactionListItemDto(
                x.tran_nr, x.time_req, x.source_node, x.dest_node, x.pan.MaskPan(), x.tran_type, x.tran_type_ext,
                x.amount_tran_req, x.receiving_inst_id, x.to_acc_number, x.merchant_id, x.terminal_id, x.resp_code_rsp))
                .ToList();

            return new PagedResultDto<TransactionListItemDto>(items, totalRecords, pageNumber, pageSize);
        }

        public async Task<TransactionDetailDto?> GetDetailAsync(long tranNr)
        {
            var entity = await _context.SwTransactions.AsNoTracking().FirstOrDefaultAsync(x => x.tran_nr == tranNr);
            if (entity == null) return null;

            TransactionDetailDto? reversal = null;
            if (entity.tran_reversed == "1")
            {
                // Mirrors legacy TransGetReversal: the reversal row's switch_key is the original
                // request's orig_data concatenated with merchant_id.
                var switchKey = $"{entity.orig_data}{entity.merchant_id}";
                var reversalEntity = await _context.SwTransactions.AsNoTracking().FirstOrDefaultAsync(x => x.switch_key == switchKey);
                if (reversalEntity != null) reversal = ToDetailDto(reversalEntity, null);
            }

            return ToDetailDto(entity, reversal);
        }

        private static TransactionDetailDto ToDetailDto(SwTransPg e, TransactionDetailDto? reversal) => new(
            e.tran_nr, e.switch_key, e.state, e.source_node, e.dest_node, e.pan.MaskPan(), e.pos_entry_mode, e.msgtype,
            e.tran_type, e.tran_type_ext, e.from_acc_type, e.to_acc_type, e.acq_inst_id, e.fwd_inst_id, e.receiving_inst_id,
            e.from_acc_number, e.to_acc_number, e.va_acc_number, e.trace_number, e.reff_number, e.source_tran, e.auth_tran,
            e.tran_reversed, e.ip_endpoint,
            e.amount_tran_req, e.amount_tran_rsp, e.amount_va, e.additional_amount, e.last_balance, e.fee_total, e.fee_swt,
            e.fee_acq, e.fee_mer, e.fee_sub, e.fee_bil, e.fee_iss, e.currency,
            e.tran_datetime, e.date_settle_req, e.date_settle_rsp, e.time_req, e.time_rsp,
            e.merchant_id, e.merchant_type, e.terminal_id,
            e.resp_code_rsp, e.resp_code_rev, e.resp_code_adv, e.icc_data, e.orig_data, e.node_data_req, e.node_data_rsp,
            reversal);
    }
}
