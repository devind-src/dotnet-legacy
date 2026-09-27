using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.Participants;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.Participants
{
    public class ParticipantService : IParticipantService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public ParticipantService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<ParticipantDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.Participants.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(p =>
                    p.participant_id.ToLower().Contains(f) ||
                    (p.name ?? "").ToLower().Contains(f) ||
                    (p.city ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(p => p.participant_id).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<ParticipantDto?> GetByIdAsync(string participantId)
        {
            var entity = await _context.Participants.AsNoTracking()
                .FirstOrDefaultAsync(p => p.participant_id == participantId);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<ParticipantDto> CreateAsync(CreateParticipantRequest request, string actingUser)
        {
            var exists = await _context.Participants.AsNoTracking()
                .AnyAsync(p => p.participant_id == request.ParticipantId);
            if (exists)
                throw new ConflictException($"Participant '{request.ParticipantId}' already exists.");

            var entity = new SwParticipant
            {
                participant_id = request.ParticipantId,
                inst_id = request.InstId,
                name = request.Name,
                address = request.Address,
                city = request.City,
                zipcode = request.Zipcode,
                person = request.Person,
                phone = request.Phone,
                fax = request.Fax,
                email = request.Email,
                virtual_account = request.VirtualAccount,
                acc_number = request.AccNumber,
                active = request.Active ? "1" : "0",
                va_name = request.VaName,
                created_by = actingUser,
                created_dt = System.DateTime.UtcNow
            };

            _context.Participants.Add(entity);
            _audit.LogInsert(entity, "sw_participant", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<ParticipantDto> UpdateAsync(string participantId, UpdateParticipantRequest request, string actingUser)
        {
            var entity = await _context.Participants.FirstOrDefaultAsync(p => p.participant_id == participantId)
                ?? throw new NotFoundException($"Participant '{participantId}' not found.");

            var before = new
            {
                entity.inst_id, entity.name, entity.address, entity.city, entity.zipcode,
                entity.person, entity.phone, entity.fax, entity.email, entity.virtual_account,
                entity.acc_number, entity.active, entity.va_name
            };

            entity.inst_id = request.InstId;
            entity.name = request.Name;
            entity.address = request.Address;
            entity.city = request.City;
            entity.zipcode = request.Zipcode;
            entity.person = request.Person;
            entity.phone = request.Phone;
            entity.fax = request.Fax;
            entity.email = request.Email;
            entity.virtual_account = request.VirtualAccount;
            entity.acc_number = request.AccNumber;
            entity.active = request.Active ? "1" : "0";
            entity.va_name = request.VaName;
            entity.updated_by = actingUser;
            entity.updated_dt = System.DateTime.UtcNow;

            _audit.LogUpdate(before, new
            {
                entity.inst_id, entity.name, entity.address, entity.city, entity.zipcode,
                entity.person, entity.phone, entity.fax, entity.email, entity.virtual_account,
                entity.acc_number, entity.active, entity.va_name
            }, "sw_participant", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(string participantId, string actingUser)
        {
            var entity = await _context.Participants.FirstOrDefaultAsync(p => p.participant_id == participantId)
                ?? throw new NotFoundException($"Participant '{participantId}' not found.");

            var inUseByMerchant = await _context.Merchants.AsNoTracking()
                .AnyAsync(m => m.participant_id == participantId);
            if (inUseByMerchant)
                throw new ConflictException($"Participant '{participantId}' is still referenced by one or more merchants and cannot be deleted.");

            _context.Participants.Remove(entity);
            _audit.LogDelete(new
            {
                entity.participant_id, entity.name
            }, "sw_participant", actingUser);

            await _context.SaveChangesAsync();
        }

        private static ParticipantDto ToDto(SwParticipant e) => new(
            e.participant_id,
            e.inst_id,
            e.name,
            e.address,
            e.city,
            e.zipcode,
            e.person,
            e.phone,
            e.fax,
            e.email,
            e.virtual_account,
            e.acc_number,
            e.active == "1",
            e.va_name);
    }
}
