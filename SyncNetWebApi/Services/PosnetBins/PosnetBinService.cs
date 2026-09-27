using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.PosnetBins;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.PosnetBins
{
    public class PosnetBinService : IPosnetBinService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public PosnetBinService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<PosnetBinDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.PosnetBins.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(b => b.participant_id.ToLower().Contains(f));
            }

            var list = await query.OrderBy(b => b.id).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<PosnetBinDto?> GetByIdAsync(int id)
        {
            var entity = await _context.PosnetBins.AsNoTracking().FirstOrDefaultAsync(b => b.id == id);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<PosnetBinDto> CreateAsync(CreatePosnetBinRequest request, string actingUser)
        {
            var participantExists = await _context.Participants.AsNoTracking()
                .AnyAsync(p => p.participant_id == request.ParticipantId);
            if (!participantExists)
                throw new ValidationException($"Participant '{request.ParticipantId}' does not exist.");

            var groupExists = await _context.CardGroups.AsNoTracking().AnyAsync(g => g.group_id == request.GroupId);
            if (!groupExists)
                throw new ValidationException($"Group '{request.GroupId}' does not exist.");

            // sw_posnet_bin.id is GENERATED ALWAYS AS IDENTITY — do not set it, the DB
            // assigns it and EF reads the value back after SaveChangesAsync (see entity note).
            var entity = new SwPosnetBin
            {
                participant_id = request.ParticipantId,
                group_id = request.GroupId,
                status = "1",
                created_by = actingUser,
                created_dt = System.DateTime.UtcNow
            };

            _context.PosnetBins.Add(entity);
            _audit.LogInsert(entity, "sw_posnet_bin", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<PosnetBinDto> UpdateAsync(int id, UpdatePosnetBinRequest request, string actingUser)
        {
            var entity = await _context.PosnetBins.FirstOrDefaultAsync(b => b.id == id)
                ?? throw new NotFoundException($"BIN '{id}' not found.");

            var groupExists = await _context.CardGroups.AsNoTracking().AnyAsync(g => g.group_id == request.GroupId);
            if (!groupExists)
                throw new ValidationException($"Group '{request.GroupId}' does not exist.");

            var before = new { entity.group_id };
            entity.group_id = request.GroupId;
            entity.updated_by = actingUser;
            entity.updated_dt = System.DateTime.UtcNow;

            _audit.LogUpdate(before, new { entity.group_id }, "sw_posnet_bin", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(int id, string actingUser)
        {
            var entity = await _context.PosnetBins.FirstOrDefaultAsync(b => b.id == id)
                ?? throw new NotFoundException($"BIN '{id}' not found.");

            _context.PosnetBins.Remove(entity);
            _audit.LogDelete(new { entity.id, entity.participant_id }, "sw_posnet_bin", actingUser);

            await _context.SaveChangesAsync();
        }

        private static PosnetBinDto ToDto(SwPosnetBin e) => new(e.id, e.participant_id, e.group_id, e.status == "1");
    }
}
