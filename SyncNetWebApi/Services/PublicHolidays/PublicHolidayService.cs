using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.PublicHolidays;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.PublicHolidays
{
    public class PublicHolidayService : IPublicHolidayService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public PublicHolidayService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<PublicHolidayDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.PublicHolidays.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(h => h.holiday_date.Contains(f) || (h.holiday_name ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(h => h.holiday_date).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<PublicHolidayDto?> GetByIdAsync(string holidayDate)
        {
            var entity = await _context.PublicHolidays.AsNoTracking().FirstOrDefaultAsync(h => h.holiday_date == holidayDate);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<PublicHolidayDto> CreateAsync(CreatePublicHolidayRequest request, string actingUser)
        {
            var exists = await _context.PublicHolidays.AsNoTracking().AnyAsync(h => h.holiday_date == request.HolidayDate);
            if (exists)
                throw new ConflictException($"Public holiday '{request.HolidayDate}' already exists.");

            var entity = new SwPublicHoliday
            {
                holiday_date = request.HolidayDate,
                holiday_name = request.HolidayName,
                status = "1",
                created_by = actingUser,
                created_dt = DateTime.UtcNow
            };

            _context.PublicHolidays.Add(entity);
            _audit.LogInsert(entity, "sw_public_holiday", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<PublicHolidayDto> UpdateAsync(string holidayDate, UpdatePublicHolidayRequest request, string actingUser)
        {
            var entity = await _context.PublicHolidays.FirstOrDefaultAsync(h => h.holiday_date == holidayDate)
                ?? throw new NotFoundException($"Public holiday '{holidayDate}' not found.");

            var before = new { entity.holiday_name, entity.status };
            entity.holiday_name = request.HolidayName;
            entity.status = request.Active ? "1" : "0";
            entity.updated_by = actingUser;
            entity.updated_dt = DateTime.UtcNow;

            _audit.LogUpdate(before, new { entity.holiday_name, entity.status }, "sw_public_holiday", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(string holidayDate, string actingUser)
        {
            var entity = await _context.PublicHolidays.FirstOrDefaultAsync(h => h.holiday_date == holidayDate)
                ?? throw new NotFoundException($"Public holiday '{holidayDate}' not found.");

            _context.PublicHolidays.Remove(entity);
            _audit.LogDelete(new { entity.holiday_date, entity.holiday_name }, "sw_public_holiday", actingUser);

            await _context.SaveChangesAsync();
        }

        private static PublicHolidayDto ToDto(SwPublicHoliday e) => new(e.holiday_date, e.holiday_name, e.status == "1");
    }
}
