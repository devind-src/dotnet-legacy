using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.SupportMembers;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.SupportMembers
{
    public class SupportMemberService : ISupportMemberService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public SupportMemberService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<SupportMemberDto>> GetRecordsAsync(string? filter = null)
        {
            var query =
                from m in _context.SupportMembers.AsNoTracking()
                join t in _context.SupportTeams.AsNoTracking() on m.team_id equals t.team_id into teams
                from t in teams.DefaultIfEmpty()
                select new { Member = m, TeamName = t == null ? null : t.name };

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(x => (x.Member.name ?? "").ToLower().Contains(f)
                    || (x.Member.email ?? "").ToLower().Contains(f)
                    || (x.Member.phone ?? "").ToLower().Contains(f)
                    || (x.TeamName ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(x => x.Member.name).ToListAsync();
            return list.Select(x => ToDto(x.Member, x.TeamName)).ToList();
        }

        public async Task<SupportMemberDto?> GetByIdAsync(int memberId)
        {
            var entity = await _context.SupportMembers.AsNoTracking().FirstOrDefaultAsync(m => m.member_id == memberId);
            if (entity == null) return null;

            var teamName = await _context.SupportTeams.AsNoTracking()
                .Where(t => t.team_id == entity.team_id).Select(t => t.name).FirstOrDefaultAsync();
            return ToDto(entity, teamName);
        }

        public async Task<SupportMemberDto> CreateAsync(CreateSupportMemberRequest request, string actingUser)
        {
            var teamExists = await _context.SupportTeams.AsNoTracking().AnyAsync(t => t.team_id == request.TeamId);
            if (!teamExists)
                throw new ValidationException($"Team '{request.TeamId}' does not exist.");

            var entity = new SwSupportMember
            {
                team_id = request.TeamId,
                name = request.Name,
                email = request.Email,
                phone = request.Phone,
                enabled = "1",
                created_by = actingUser,
                created_dt = DateTime.UtcNow
            };

            _context.SupportMembers.Add(entity);
            _audit.LogInsert(entity, "sw_support_members", actingUser);

            await _context.SaveChangesAsync();

            var teamName = await _context.SupportTeams.AsNoTracking()
                .Where(t => t.team_id == entity.team_id).Select(t => t.name).FirstOrDefaultAsync();
            return ToDto(entity, teamName);
        }

        public async Task<SupportMemberDto> UpdateAsync(int memberId, UpdateSupportMemberRequest request, string actingUser)
        {
            var entity = await _context.SupportMembers.FirstOrDefaultAsync(m => m.member_id == memberId)
                ?? throw new NotFoundException($"Support member '{memberId}' not found.");

            var teamExists = await _context.SupportTeams.AsNoTracking().AnyAsync(t => t.team_id == request.TeamId);
            if (!teamExists)
                throw new ValidationException($"Team '{request.TeamId}' does not exist.");

            var before = new { entity.team_id, entity.name, entity.email, entity.phone, entity.enabled };
            entity.team_id = request.TeamId;
            entity.name = request.Name;
            entity.email = request.Email;
            entity.phone = request.Phone;
            entity.enabled = request.Active ? "1" : "0";
            entity.updated_by = actingUser;
            entity.updated_dt = DateTime.UtcNow;

            _audit.LogUpdate(before, new { entity.team_id, entity.name, entity.email, entity.phone, entity.enabled }, "sw_support_members", actingUser);

            await _context.SaveChangesAsync();

            var teamName = await _context.SupportTeams.AsNoTracking()
                .Where(t => t.team_id == entity.team_id).Select(t => t.name).FirstOrDefaultAsync();
            return ToDto(entity, teamName);
        }

        public async Task DeleteAsync(int memberId, string actingUser)
        {
            var entity = await _context.SupportMembers.FirstOrDefaultAsync(m => m.member_id == memberId)
                ?? throw new NotFoundException($"Support member '{memberId}' not found.");

            _context.SupportMembers.Remove(entity);
            _audit.LogDelete(new { entity.member_id, entity.name }, "sw_support_members", actingUser);

            await _context.SaveChangesAsync();
        }

        private static SupportMemberDto ToDto(SwSupportMember e, string? teamName) =>
            new(e.member_id, e.team_id, teamName, e.name, e.email, e.phone, e.enabled == "1");
    }
}
