using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.BusinessDates;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.BusinessDates
{
    public class BusinessDateService : IBusinessDateService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public BusinessDateService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<BusinessDateDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.BusinessDates.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(b => b.business_calendar.ToLower().Contains(f));
            }

            var list = await query.OrderBy(b => b.business_calendar).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<BusinessDateDto?> GetByIdAsync(string businessCalendar)
        {
            var entity = await _context.BusinessDates.AsNoTracking().FirstOrDefaultAsync(b => b.business_calendar == businessCalendar);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<BusinessDateDto> CreateAsync(CreateBusinessDateRequest request, string actingUser)
        {
            var exists = await _context.BusinessDates.AsNoTracking().AnyAsync(b => b.business_calendar == request.BusinessCalendar);
            if (exists)
                throw new ConflictException($"Business calendar '{request.BusinessCalendar}' already exists.");

            // current_bsn_date/previous_bsn_date are intentionally left null — managed by an
            // external batch/EOD process, not by this API (see entity note).
            var entity = new SwBusinessDate
            {
                business_calendar = request.BusinessCalendar,
                time_cutover = request.TimeCutover,
                enable_closing = request.EnableClosing ? "1" : "0",
                time_start = request.TimeStart,
                time_end = request.TimeEnd,
                sun = request.Sun ? "1" : "0",
                mon = request.Mon ? "1" : "0",
                tue = request.Tue ? "1" : "0",
                wed = request.Wed ? "1" : "0",
                thu = request.Thu ? "1" : "0",
                fri = request.Fri ? "1" : "0",
                sat = request.Sat ? "1" : "0",
                status = "1",
                created_by = actingUser,
                created_dt = DateTime.UtcNow
            };

            _context.BusinessDates.Add(entity);
            _audit.LogInsert(entity, "sw_business_date", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<BusinessDateDto> UpdateAsync(string businessCalendar, UpdateBusinessDateRequest request, string actingUser)
        {
            var entity = await _context.BusinessDates.FirstOrDefaultAsync(b => b.business_calendar == businessCalendar)
                ?? throw new NotFoundException($"Business calendar '{businessCalendar}' not found.");

            var before = new { entity.time_cutover, entity.enable_closing, entity.time_start, entity.time_end, entity.sun, entity.mon, entity.tue, entity.wed, entity.thu, entity.fri, entity.sat, entity.status };

            // current_bsn_date/previous_bsn_date are deliberately untouched here (see entity note).
            entity.time_cutover = request.TimeCutover;
            entity.enable_closing = request.EnableClosing ? "1" : "0";
            entity.time_start = request.TimeStart;
            entity.time_end = request.TimeEnd;
            entity.sun = request.Sun ? "1" : "0";
            entity.mon = request.Mon ? "1" : "0";
            entity.tue = request.Tue ? "1" : "0";
            entity.wed = request.Wed ? "1" : "0";
            entity.thu = request.Thu ? "1" : "0";
            entity.fri = request.Fri ? "1" : "0";
            entity.sat = request.Sat ? "1" : "0";
            entity.status = request.Active ? "1" : "0";
            entity.updated_by = actingUser;
            entity.updated_dt = DateTime.UtcNow;

            _audit.LogUpdate(before, new { entity.time_cutover, entity.enable_closing, entity.time_start, entity.time_end, entity.sun, entity.mon, entity.tue, entity.wed, entity.thu, entity.fri, entity.sat, entity.status }, "sw_business_date", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(string businessCalendar, string actingUser)
        {
            var entity = await _context.BusinessDates.FirstOrDefaultAsync(b => b.business_calendar == businessCalendar)
                ?? throw new NotFoundException($"Business calendar '{businessCalendar}' not found.");

            _context.BusinessDates.Remove(entity);
            _audit.LogDelete(new { entity.business_calendar }, "sw_business_date", actingUser);

            await _context.SaveChangesAsync();
        }

        private static BusinessDateDto ToDto(SwBusinessDate e) => new(
            e.business_calendar,
            e.time_cutover,
            e.current_bsn_date,
            e.previous_bsn_date,
            e.enable_closing == "1",
            e.time_start,
            e.time_end,
            e.sun == "1",
            e.mon == "1",
            e.tue == "1",
            e.wed == "1",
            e.thu == "1",
            e.fri == "1",
            e.sat == "1",
            e.status == "1");
    }
}
