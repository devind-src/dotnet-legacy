using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.CardGroups;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.CardGroups
{
    public class CardGroupService : ICardGroupService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public CardGroupService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<CardGroupDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.CardGroups.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(g => (g.group_name ?? "").ToLower().Contains(f) || (g.inst_id ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(g => g.group_name).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<CardGroupDto?> GetByIdAsync(int groupId)
        {
            var entity = await _context.CardGroups.AsNoTracking().FirstOrDefaultAsync(g => g.group_id == groupId);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<CardGroupDto> CreateAsync(CreateCardGroupRequest request, string actingUser)
        {
            var entity = new SwGroup
            {
                group_name = request.GroupName,
                inst_id = request.InstId
            };

            _context.CardGroups.Add(entity);
            _audit.LogInsert(entity, "sw_group", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<CardGroupDto> UpdateAsync(int groupId, UpdateCardGroupRequest request, string actingUser)
        {
            var entity = await _context.CardGroups.FirstOrDefaultAsync(g => g.group_id == groupId)
                ?? throw new NotFoundException($"Card group '{groupId}' not found.");

            var before = new { entity.inst_id };
            entity.inst_id = request.InstId;

            _audit.LogUpdate(before, new { entity.inst_id }, "sw_group", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(int groupId, string actingUser)
        {
            var entity = await _context.CardGroups.FirstOrDefaultAsync(g => g.group_id == groupId)
                ?? throw new NotFoundException($"Card group '{groupId}' not found.");

            if (await _context.CardBins.AsNoTracking().AnyAsync(b => b.group_id == groupId))
                throw new ConflictException($"Card group '{groupId}' is still referenced by one or more BINs and cannot be deleted.");

            if (await _context.CardAccounts.AsNoTracking().AnyAsync(a => a.group_id == groupId))
                throw new ConflictException($"Card group '{groupId}' is still referenced by one or more accounts and cannot be deleted.");

            _context.CardGroups.Remove(entity);
            _audit.LogDelete(new { entity.group_id, entity.group_name }, "sw_group", actingUser);

            await _context.SaveChangesAsync();
        }

        private static CardGroupDto ToDto(SwGroup e) => new(e.group_id, e.group_name, e.inst_id);
    }
}
