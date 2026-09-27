using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.SubMerchantGroups;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.SubMerchantGroups
{
    public class SubMerchantGroupService : ISubMerchantGroupService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public SubMerchantGroupService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<SubMerchantGroupDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.SubMerchantGroups.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(g => g.group_name.ToLower().Contains(f));
            }

            var list = await query.OrderBy(g => g.group_name).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<SubMerchantGroupDto?> GetByNameAsync(string groupName)
        {
            var entity = await _context.SubMerchantGroups.AsNoTracking()
                .FirstOrDefaultAsync(g => g.group_name == groupName);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<SubMerchantGroupDto> CreateAsync(CreateSubMerchantGroupRequest request, string actingUser)
        {
            var exists = await _context.SubMerchantGroups.AsNoTracking()
                .AnyAsync(g => g.group_name == request.GroupName);
            if (exists)
                throw new ConflictException($"Sub merchant group '{request.GroupName}' already exists.");

            var participantExists = await _context.Participants.AsNoTracking()
                .AnyAsync(p => p.participant_id == request.ParticipantId);
            if (!participantExists)
                throw new ValidationException($"Participant '{request.ParticipantId}' does not exist.");

            var parentExists = await _context.MerchantGroups.AsNoTracking()
                .AnyAsync(g => g.group_name == request.ParentGroupName);
            if (!parentExists)
                throw new ValidationException($"Merchant group '{request.ParentGroupName}' does not exist.");

            var entity = new SwSubMerchantGroup
            {
                group_name = request.GroupName,
                participant_id = request.ParticipantId,
                parrent_id = request.ParentGroupName,
                notes = request.Notes
            };

            _context.SubMerchantGroups.Add(entity);
            _audit.LogInsert(entity, "sw_submerchant_group", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<SubMerchantGroupDto> UpdateAsync(string groupName, UpdateSubMerchantGroupRequest request, string actingUser)
        {
            var entity = await _context.SubMerchantGroups.FirstOrDefaultAsync(g => g.group_name == groupName)
                ?? throw new NotFoundException($"Sub merchant group '{groupName}' not found.");

            var parentExists = await _context.MerchantGroups.AsNoTracking()
                .AnyAsync(g => g.group_name == request.ParentGroupName);
            if (!parentExists)
                throw new ValidationException($"Merchant group '{request.ParentGroupName}' does not exist.");

            var before = new { entity.parrent_id, entity.notes };
            entity.parrent_id = request.ParentGroupName;
            entity.notes = request.Notes;
            _audit.LogUpdate(before, new { entity.parrent_id, entity.notes }, "sw_submerchant_group", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(string groupName, string actingUser)
        {
            var entity = await _context.SubMerchantGroups.FirstOrDefaultAsync(g => g.group_name == groupName)
                ?? throw new NotFoundException($"Sub merchant group '{groupName}' not found.");

            if (await _context.Terminals.AsNoTracking().AnyAsync(t => t.subgroup_name == groupName))
                throw new ConflictException($"Sub merchant group '{groupName}' is still referenced by one or more terminals and cannot be deleted.");
            if (await _context.TerminalClients.AsNoTracking().AnyAsync(c => c.subgroup_name == groupName))
                throw new ConflictException($"Sub merchant group '{groupName}' is still referenced by one or more credentials and cannot be deleted.");
            if (await _context.SoundBoxTerminals.AsNoTracking().AnyAsync(s => s.subgroup_name == groupName))
                throw new ConflictException($"Sub merchant group '{groupName}' is still referenced by one or more soundbox terminals and cannot be deleted.");

            _context.SubMerchantGroups.Remove(entity);
            _audit.LogDelete(new { entity.group_name }, "sw_submerchant_group", actingUser);

            await _context.SaveChangesAsync();
        }

        private static SubMerchantGroupDto ToDto(SwSubMerchantGroup e) => new(e.group_name, e.participant_id, e.parrent_id, e.notes);
    }
}
