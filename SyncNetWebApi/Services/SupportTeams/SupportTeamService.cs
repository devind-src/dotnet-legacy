using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.SupportTeams;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.SupportTeams
{
    public class SupportTeamService : ISupportTeamService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public SupportTeamService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<SupportTeamDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.SupportTeams.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(t => (t.name ?? "").ToLower().Contains(f) || (t.region ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(t => t.name).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<SupportTeamDto?> GetByIdAsync(int teamId)
        {
            var entity = await _context.SupportTeams.AsNoTracking().FirstOrDefaultAsync(t => t.team_id == teamId);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<SupportTeamDto> CreateAsync(CreateSupportTeamRequest request, string actingUser)
        {
            var entity = new SwSupportTeam
            {
                name = request.Name,
                region = request.Region,
                status = "1",
                created_by = actingUser,
                created_dt = DateTime.UtcNow
            };

            _context.SupportTeams.Add(entity);
            _audit.LogInsert(entity, "sw_support_teams", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<SupportTeamDto> UpdateAsync(int teamId, UpdateSupportTeamRequest request, string actingUser)
        {
            var entity = await _context.SupportTeams.FirstOrDefaultAsync(t => t.team_id == teamId)
                ?? throw new NotFoundException($"Support team '{teamId}' not found.");

            var before = new { entity.region, entity.status };
            entity.region = request.Region;
            entity.status = request.Active ? "1" : "0";
            entity.updated_by = actingUser;
            entity.updated_dt = DateTime.UtcNow;

            _audit.LogUpdate(before, new { entity.region, entity.status }, "sw_support_teams", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(int teamId, string actingUser)
        {
            var entity = await _context.SupportTeams.FirstOrDefaultAsync(t => t.team_id == teamId)
                ?? throw new NotFoundException($"Support team '{teamId}' not found.");

            if (await _context.SupportMembers.AsNoTracking().AnyAsync(m => m.team_id == teamId))
                throw new ConflictException($"Support team '{teamId}' is still referenced by one or more members and cannot be deleted.");

            _context.SupportTeams.Remove(entity);
            _audit.LogDelete(new { entity.team_id, entity.name }, "sw_support_teams", actingUser);

            await _context.SaveChangesAsync();
        }

        private static SupportTeamDto ToDto(SwSupportTeam e) => new(e.team_id, e.name, e.region, e.status == "1");
    }
}
