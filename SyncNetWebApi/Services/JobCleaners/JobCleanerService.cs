using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.JobCleaners;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.JobCleaners
{
    public class JobCleanerService : IJobCleanerService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public JobCleanerService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<JobCleanerDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.Cleaners.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(c => c.entity.ToLower().Contains(f) || (c.description ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(c => c.entity).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<JobCleanerDto?> GetByIdAsync(string entity)
        {
            var row = await _context.Cleaners.AsNoTracking().FirstOrDefaultAsync(c => c.entity == entity);
            return row == null ? null : ToDto(row);
        }

        public async Task<JobCleanerDto> CreateAsync(CreateJobCleanerRequest request, string actingUser)
        {
            var exists = await _context.Cleaners.AsNoTracking().AnyAsync(c => c.entity == request.Entity);
            if (exists)
                throw new ConflictException($"Job cleaner '{request.Entity}' already exists.");

            var row = new SwCleaner
            {
                entity = request.Entity,
                period = request.Period,
                description = request.Description,
                status = "1",
                created_by = actingUser,
                created_dt = DateTime.UtcNow
            };

            _context.Cleaners.Add(row);
            _audit.LogInsert(row, "sw_cleaner", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(row);
        }

        public async Task<JobCleanerDto> UpdateAsync(string entity, UpdateJobCleanerRequest request, string actingUser)
        {
            var row = await _context.Cleaners.FirstOrDefaultAsync(c => c.entity == entity)
                ?? throw new NotFoundException($"Job cleaner '{entity}' not found.");

            var before = new { row.period, row.description, row.status };
            row.period = request.Period;
            row.description = request.Description;
            row.status = request.Active ? "1" : "0";
            row.updated_by = actingUser;
            row.updated_dt = DateTime.UtcNow;

            _audit.LogUpdate(before, new { row.period, row.description, row.status }, "sw_cleaner", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(row);
        }

        public async Task DeleteAsync(string entity, string actingUser)
        {
            var row = await _context.Cleaners.FirstOrDefaultAsync(c => c.entity == entity)
                ?? throw new NotFoundException($"Job cleaner '{entity}' not found.");

            _context.Cleaners.Remove(row);
            _audit.LogDelete(new { row.entity }, "sw_cleaner", actingUser);

            await _context.SaveChangesAsync();
        }

        private static JobCleanerDto ToDto(SwCleaner e) => new(e.entity, e.period, e.description, e.status == "1");
    }
}
