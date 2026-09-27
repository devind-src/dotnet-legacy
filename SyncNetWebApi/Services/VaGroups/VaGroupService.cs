using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.VaGroups;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.VaGroups
{
    public class VaGroupService : IVaGroupService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public VaGroupService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<VaGroupDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.VaGroups.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(g => g.group_name.ToLower().Contains(f));
            }

            var list = await query.OrderBy(g => g.group_name).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<VaGroupDto?> GetByIdAsync(string groupName)
        {
            var entity = await _context.VaGroups.AsNoTracking().FirstOrDefaultAsync(g => g.group_name == groupName);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<VaGroupDto> CreateAsync(CreateVaGroupRequest request, string actingUser)
        {
            if (await _context.VaGroups.AsNoTracking().AnyAsync(g => g.group_name == request.GroupName))
                throw new ConflictException($"VA group '{request.GroupName}' already exists.");

            var entity = new VaGroup
            {
                group_name = request.GroupName,
                notes = request.Notes
            };

            _context.VaGroups.Add(entity);
            _audit.LogInsert(entity, "va_group", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<VaGroupDto> UpdateAsync(string groupName, UpdateVaGroupRequest request, string actingUser)
        {
            var entity = await _context.VaGroups.FirstOrDefaultAsync(g => g.group_name == groupName)
                ?? throw new NotFoundException($"VA group '{groupName}' not found.");

            var before = new { entity.notes };
            entity.notes = request.Notes;

            _audit.LogUpdate(before, new { entity.notes }, "va_group", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(string groupName, string actingUser)
        {
            var entity = await _context.VaGroups.FirstOrDefaultAsync(g => g.group_name == groupName)
                ?? throw new NotFoundException($"VA group '{groupName}' not found.");

            if (await _context.VaAccounts.AsNoTracking().AnyAsync(a => a.group_name == groupName))
                throw new ConflictException($"VA group '{groupName}' is still referenced by one or more accounts and cannot be deleted.");

            _context.VaGroups.Remove(entity);
            _audit.LogDelete(entity, "va_group", actingUser);

            await _context.SaveChangesAsync();
        }

        private static VaGroupDto ToDto(VaGroup e) => new(e.group_name, e.notes);
    }
}
