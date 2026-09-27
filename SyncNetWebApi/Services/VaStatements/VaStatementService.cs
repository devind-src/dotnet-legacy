using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Data;
using SyncNetApi.Dtos.VaTransRequests;

namespace SyncNetApi.Services.VaStatements
{
    /// <summary>Virtual Account &gt; Statement — read-only report over sw_trans_pg (the core
    /// switch ledger) joined to va_account. Mirrors legacy's VaTransStatementSql, which is the
    /// version actually wired to Statement/Index.razor (a second LINQ-only variant,
    /// VaTransStatement, exists in legacy but is dead code — not replicated).</summary>
    public class VaStatementService : IVaStatementService
    {
        private static readonly string[] TranTypes = { "VDB", "VCR", "WDL", "DEP", "TRF", "PUR", "PAY" };
        private static readonly string[] RespCodes = { "00", "68", "0000", "1068", "1011" };

        private readonly SyncNetDbContext _context;

        public VaStatementService(SyncNetDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<VaStatementDto>> GetStatementAsync(DateTime dateStart, DateTime dateEnd, string? vaName)
        {
            // sw_trans_pg.time_req is "timestamp without time zone" (naive wall-clock, written
            // via DateTime.Now — see VaTransRequestService.ApproveAsync). ASP.NET's default
            // DateTime model binder parses a query string with a "+07:00" offset (what the WASM
            // client sends, DateTime.ToString("O") on a Local DateTime) into Kind=Utc — Npgsql
            // then refuses to write a Utc-kind value against a "without time zone" column
            // ("Cannot write DateTime with Kind=UTC..."). Strip the Kind so the wall-clock value
            // is compared as-is, same fix class as Phase P6 (sw_fees_promo.date_start/date_end).
            dateStart = DateTime.SpecifyKind(dateStart, DateTimeKind.Unspecified);
            dateEnd = DateTime.SpecifyKind(dateEnd, DateTimeKind.Unspecified);

            var query = from x in _context.SwTransactions.AsNoTracking()
                        join y in _context.VaAccounts.AsNoTracking() on x.va_acc_number equals y.acc_nr
                        where x.time_req >= dateStart && x.time_req <= dateEnd
                              && TranTypes.Contains(x.tran_type)
                              && RespCodes.Contains(x.resp_code_rsp)
                        select new { Trans = x, VaName = y.va_name, GroupName = y.group_name };

            if (!string.IsNullOrWhiteSpace(vaName))
                query = query.Where(r => r.VaName == vaName);

            var rows = await query.OrderByDescending(r => r.Trans.tran_nr).ToListAsync();

            var result = new List<VaStatementDto>(rows.Count);

            foreach (var r in rows)
            {
                var x = r.Trans;
                var amount = x.amount_va ?? 0;
                var lastBalance = x.last_balance ?? 0;

                result.Add(new VaStatementDto(
                    x.tran_nr, r.VaName, x.va_acc_number, r.GroupName, x.time_req, x.tran_type,
                    x.receiving_inst_id, x.to_acc_number, x.reff_number, amount, lastBalance));

                // Mirrors legacy: a reversed transaction gets a synthetic "REV" row inserted
                // right after it, with balance = original last_balance + amount (the balance
                // as if the reversal had not happened).
                if (x.tran_reversed == "1")
                {
                    result.Add(new VaStatementDto(
                        x.tran_nr, r.VaName, x.va_acc_number, r.GroupName, x.time_req, "REV",
                        x.receiving_inst_id, x.to_acc_number, x.reff_number, amount, lastBalance + amount));
                }
            }

            return result;
        }
    }
}
