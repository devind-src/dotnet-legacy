using Microsoft.EntityFrameworkCore;
using SyncNetApi.Data;
using SyncNetApi.Dtos.Common;
using SyncNetApi.Dtos.Queries;

namespace SyncNetApi.Services.Queries
{
    /// <summary>Read-only over sw_audit — written by every module's Create/Update/Delete via
    /// IAuditService (see PROJECT_TECHNICAL_SUMMARY.md §4). Mirrors legacy
    /// DbSwitchNetService.AuditGetRecords/AuditGetRow (Queries &gt; Audit Trail). TableName/
    /// UserName filters are exact match, same as legacy (not partial/Contains).</summary>
    public class QueryAuditService : IQueryAuditService
    {
        private readonly SyncNetDbContext _context;

        public QueryAuditService(SyncNetDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResultDto<AuditListItemDto>> SearchAsync(AuditSearchRequest request)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize is < 1 or > 100 ? 25 : request.PageSize;

            var query = _context.Audits.AsNoTracking().AsQueryable();

            // updatedate is "timestamp without time zone" — strip Kind regardless of what the
            // client sent, same reasoning as QueryTransactionService (see SwTransPg's doc
            // comment / VA Statement §7.21).
            var dateStart = request.DateStart.HasValue ? DateTime.SpecifyKind(request.DateStart.Value, DateTimeKind.Unspecified) : (DateTime?)null;
            var dateEnd = request.DateEnd.HasValue ? DateTime.SpecifyKind(request.DateEnd.Value, DateTimeKind.Unspecified) : (DateTime?)null;

            if (dateStart.HasValue) query = query.Where(x => x.updatedate >= dateStart);
            if (dateEnd.HasValue) query = query.Where(x => x.updatedate <= dateEnd);
            if (!string.IsNullOrWhiteSpace(request.TableName)) query = query.Where(x => x.tablename == request.TableName);
            if (!string.IsNullOrWhiteSpace(request.UserName)) query = query.Where(x => x.username == request.UserName);

            query = query.OrderByDescending(x => x.id);

            var totalRecords = await query.LongCountAsync();

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new AuditListItemDto(x.id, x.type, x.tablename, x.username, x.updatedate))
                .ToListAsync();

            return new PagedResultDto<AuditListItemDto>(items, totalRecords, pageNumber, pageSize);
        }

        public async Task<AuditDetailDto?> GetDetailAsync(long id)
        {
            var entity = await _context.Audits.AsNoTracking().FirstOrDefaultAsync(x => x.id == id);
            return entity == null
                ? null
                : new AuditDetailDto(entity.id, entity.type, entity.tablename, entity.username, entity.updatedate, entity.oldvalue, entity.newvalue);
        }
    }
}
