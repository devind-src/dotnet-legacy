using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.MerchantGroups;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.MerchantGroups
{
    public class MerchantGroupService : IMerchantGroupService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public MerchantGroupService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<MerchantGroupDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.MerchantGroups.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(g => g.group_name.ToLower().Contains(f));
            }

            var list = await query.OrderBy(g => g.group_name).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<MerchantGroupDto?> GetByNameAsync(string groupName)
        {
            var entity = await _context.MerchantGroups.AsNoTracking()
                .FirstOrDefaultAsync(g => g.group_name == groupName);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<MerchantGroupDto> CreateAsync(CreateMerchantGroupRequest request, string actingUser)
        {
            var exists = await _context.MerchantGroups.AsNoTracking()
                .AnyAsync(g => g.group_name == request.GroupName);
            if (exists)
                throw new ConflictException($"Merchant group '{request.GroupName}' already exists.");

            var participantExists = await _context.Participants.AsNoTracking()
                .AnyAsync(p => p.participant_id == request.ParticipantId);
            if (!participantExists)
                throw new ValidationException($"Participant '{request.ParticipantId}' does not exist.");

            var entity = new SwMerchantGroup
            {
                group_name = request.GroupName,
                participant_id = request.ParticipantId,
                notes = request.Notes
            };

            _context.MerchantGroups.Add(entity);
            _audit.LogInsert(entity, "sw_merchant_group", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<MerchantGroupDto> UpdateAsync(string groupName, UpdateMerchantGroupRequest request, string actingUser)
        {
            var entity = await _context.MerchantGroups.FirstOrDefaultAsync(g => g.group_name == groupName)
                ?? throw new NotFoundException($"Merchant group '{groupName}' not found.");

            var before = new { entity.notes };
            entity.notes = request.Notes;
            _audit.LogUpdate(before, new { entity.notes }, "sw_merchant_group", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(string groupName, string actingUser)
        {
            var entity = await _context.MerchantGroups.FirstOrDefaultAsync(g => g.group_name == groupName)
                ?? throw new NotFoundException($"Merchant group '{groupName}' not found.");

            if (await _context.Merchants.AsNoTracking().AnyAsync(m => m.group_name == groupName))
                throw new ConflictException($"Merchant group '{groupName}' is still referenced by one or more merchants and cannot be deleted.");
            if (await _context.Terminals.AsNoTracking().AnyAsync(t => t.group_name == groupName))
                throw new ConflictException($"Merchant group '{groupName}' is still referenced by one or more terminals and cannot be deleted.");
            if (await _context.TerminalClients.AsNoTracking().AnyAsync(c => c.group_name == groupName))
                throw new ConflictException($"Merchant group '{groupName}' is still referenced by one or more credentials and cannot be deleted.");
            if (await _context.TerminalLimits.AsNoTracking().AnyAsync(l => l.group_name == groupName))
                throw new ConflictException($"Merchant group '{groupName}' is still referenced by one or more limits and cannot be deleted.");
            if (await _context.SoundBoxTerminals.AsNoTracking().AnyAsync(s => s.group_name == groupName))
                throw new ConflictException($"Merchant group '{groupName}' is still referenced by one or more soundbox terminals and cannot be deleted.");
            if (await _context.SubMerchantGroups.AsNoTracking().AnyAsync(s => s.parrent_id == groupName))
                throw new ConflictException($"Merchant group '{groupName}' is still referenced by one or more sub merchant groups and cannot be deleted.");

            _context.MerchantGroups.Remove(entity);
            _audit.LogDelete(new { entity.group_name }, "sw_merchant_group", actingUser);

            await _context.SaveChangesAsync();
        }

        private static MerchantGroupDto ToDto(SwMerchantGroup e) => new(e.group_name, e.participant_id, e.notes);
    }
}
