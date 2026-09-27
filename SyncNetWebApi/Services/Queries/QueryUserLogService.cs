using Microsoft.EntityFrameworkCore;
using SyncNetApi.Data;
using SyncNetApi.Dtos.Common;
using SyncNetApi.Dtos.Queries;

namespace SyncNetApi.Services.Queries
{
    /// <summary>Read-only over dashboard_user_log — written on every login/logout since Phase 1
    /// (see AuthController.LogActivity). Mirrors legacy DbSwitchNetService.UserLogGetRecords
    /// (Queries &gt; User Log). No detail endpoint — legacy's "Detail" dialog reference on this
    /// page is dead code (no click handler ever opens it, and no Detail.razor exists for
    /// UserLog), and every field here is already a complete atomic record with nothing to
    /// drill into. State filter is new here (legacy only filters by UserName) — a UI-only
    /// addition, no schema change.</summary>
    public class QueryUserLogService : IQueryUserLogService
    {
        private readonly SyncNetDbContext _context;

        public QueryUserLogService(SyncNetDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResultDto<UserLogItemDto>> SearchAsync(UserLogSearchRequest request)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize is < 1 or > 100 ? 25 : request.PageSize;

            var query = _context.UserLog.AsNoTracking().AsQueryable();

            // date_time is "timestamp without time zone" — strip Kind regardless of what the
            // client sent, same reasoning as QueryTransactionService (see SwTransPg's doc
            // comment / VA Statement §7.21).
            var dateStart = request.DateStart.HasValue ? DateTime.SpecifyKind(request.DateStart.Value, DateTimeKind.Unspecified) : (DateTime?)null;
            var dateEnd = request.DateEnd.HasValue ? DateTime.SpecifyKind(request.DateEnd.Value, DateTimeKind.Unspecified) : (DateTime?)null;

            if (dateStart.HasValue) query = query.Where(x => x.date_time >= dateStart);
            if (dateEnd.HasValue) query = query.Where(x => x.date_time <= dateEnd);
            if (!string.IsNullOrWhiteSpace(request.UserName)) query = query.Where(x => x.user_name == request.UserName);
            if (!string.IsNullOrWhiteSpace(request.State)) query = query.Where(x => x.state == request.State);

            query = query.OrderByDescending(x => x.log_id);

            var totalRecords = await query.LongCountAsync();

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new UserLogItemDto(x.log_id, x.user_name, x.state, x.date_time, x.host_address, x.host_name, x.host_agent))
                .ToListAsync();

            return new PagedResultDto<UserLogItemDto>(items, totalRecords, pageNumber, pageSize);
        }
    }
}
