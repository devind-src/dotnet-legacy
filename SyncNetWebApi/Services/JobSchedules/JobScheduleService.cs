using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.JobSchedules;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.JobSchedules
{
    public class JobScheduleService : IJobScheduleService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public JobScheduleService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<JobScheduleDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.Jobs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(j => (j.job_name ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(j => j.job_name).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<JobScheduleDto?> GetByIdAsync(int jobId)
        {
            var entity = await _context.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.job_id == jobId);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<JobScheduleDto> CreateAsync(CreateJobScheduleRequest request, string actingUser)
        {
            var entity = new SwJob
            {
                job_name = request.JobName,
                freq_flag = request.FreqFlag,
                freq_once = request.FreqOnce,
                freq_number = request.FreqNumber,
                freq_start = request.FreqStart,
                freq_end = request.FreqEnd,
                run_at = request.RunAt,
                app_path = request.AppPath,
                enabled = "1",
                created_by = actingUser,
                created_dt = DateTime.UtcNow
            };

            _context.Jobs.Add(entity);
            _audit.LogInsert(entity, "sw_jobs", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<JobScheduleDto> UpdateAsync(int jobId, UpdateJobScheduleRequest request, string actingUser)
        {
            var entity = await _context.Jobs.FirstOrDefaultAsync(j => j.job_id == jobId)
                ?? throw new NotFoundException($"Job '{jobId}' not found.");

            var before = new { entity.freq_flag, entity.freq_once, entity.freq_number, entity.freq_start, entity.freq_end, entity.run_at, entity.app_path, entity.enabled };

            // last_running/status/read_only are deliberately untouched here (see entity note).
            entity.freq_flag = request.FreqFlag;
            entity.freq_once = request.FreqOnce;
            entity.freq_number = request.FreqNumber;
            entity.freq_start = request.FreqStart;
            entity.freq_end = request.FreqEnd;
            entity.run_at = request.RunAt;
            entity.app_path = request.AppPath;
            entity.enabled = request.Active ? "1" : "0";
            entity.updated_by = actingUser;
            entity.updated_dt = DateTime.UtcNow;

            _audit.LogUpdate(before, new { entity.freq_flag, entity.freq_once, entity.freq_number, entity.freq_start, entity.freq_end, entity.run_at, entity.app_path, entity.enabled }, "sw_jobs", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(int jobId, string actingUser)
        {
            var entity = await _context.Jobs.FirstOrDefaultAsync(j => j.job_id == jobId)
                ?? throw new NotFoundException($"Job '{jobId}' not found.");

            _context.Jobs.Remove(entity);
            _audit.LogDelete(new { entity.job_id, entity.job_name }, "sw_jobs", actingUser);

            await _context.SaveChangesAsync();
        }

        private static JobScheduleDto ToDto(SwJob e) => new(
            e.job_id,
            e.job_name,
            e.freq_flag,
            e.freq_once,
            e.freq_number,
            e.freq_start,
            e.freq_end,
            e.enabled == "1",
            e.run_at,
            e.app_path,
            e.last_running,
            e.status,
            e.read_only == "1");
    }
}
