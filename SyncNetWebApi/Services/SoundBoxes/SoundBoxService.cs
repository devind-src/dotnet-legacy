using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.SoundBoxes;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.SoundBoxes
{
    public class SoundBoxService : ISoundBoxService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public SoundBoxService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<SoundBoxDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.SoundBoxTerminals.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(s =>
                    s.nmid.ToLower().Contains(f) ||
                    (s.store_name ?? "").ToLower().Contains(f) ||
                    (s.serial_number ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(s => s.nmid).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<SoundBoxDto?> GetByIdAsync(string nmid)
        {
            var entity = await _context.SoundBoxTerminals.AsNoTracking().FirstOrDefaultAsync(s => s.nmid == nmid);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<SoundBoxDto> CreateAsync(CreateSoundBoxRequest request, string actingUser)
        {
            var duplicate = await _context.SoundBoxTerminals.AsNoTracking()
                .AnyAsync(s => s.nmid == request.Nmid && s.serial_number == request.SerialNumber);
            if (duplicate)
                throw new ConflictException("NMID & serial number combination already exists.");

            var entity = new SoundBoxTerminal
            {
                nmid = request.Nmid,
                store_name = request.StoreName,
                address = request.Address,
                city = request.City,
                serial_number = request.SerialNumber,
                provider = request.Provider,
                group_name = request.GroupName,
                subgroup_name = request.SubgroupName
            };

            _context.SoundBoxTerminals.Add(entity);
            _audit.LogInsert(entity, "soundbox_terminal", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<SoundBoxDto> UpdateAsync(string nmid, UpdateSoundBoxRequest request, string actingUser)
        {
            var entity = await _context.SoundBoxTerminals.FirstOrDefaultAsync(s => s.nmid == nmid)
                ?? throw new NotFoundException($"SoundBox '{nmid}' not found.");

            var before = ToAuditSnapshot(entity);

            entity.store_name = request.StoreName;
            entity.address = request.Address;
            entity.city = request.City;
            entity.serial_number = request.SerialNumber;
            entity.provider = request.Provider;
            entity.group_name = request.GroupName;
            entity.subgroup_name = request.SubgroupName;

            _audit.LogUpdate(before, ToAuditSnapshot(entity), "soundbox_terminal", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(string nmid, string actingUser)
        {
            var entity = await _context.SoundBoxTerminals.FirstOrDefaultAsync(s => s.nmid == nmid)
                ?? throw new NotFoundException($"SoundBox '{nmid}' not found.");

            _context.SoundBoxTerminals.Remove(entity);
            _audit.LogDelete(new { entity.nmid, entity.store_name }, "soundbox_terminal", actingUser);

            await _context.SaveChangesAsync();
        }

        private static object ToAuditSnapshot(SoundBoxTerminal e) => new
        {
            e.store_name, e.address, e.city, e.serial_number, e.provider, e.group_name, e.subgroup_name
        };

        private static SoundBoxDto ToDto(SoundBoxTerminal e) => new(
            e.nmid, e.store_name ?? string.Empty, e.address, e.city, e.serial_number ?? string.Empty,
            e.provider ?? string.Empty, e.group_name, e.subgroup_name);
    }
}
