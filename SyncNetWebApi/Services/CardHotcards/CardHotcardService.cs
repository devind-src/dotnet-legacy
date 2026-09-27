using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.CardHotcards;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.CardHotcards
{
    public class CardHotcardService : ICardHotcardService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public CardHotcardService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<CardHotcardDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.CardHotcards.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(h => h.card_nr.ToLower().Contains(f)
                    || (h.resp_code ?? "").ToLower().Contains(f)
                    || (h.auth_id_resp ?? "").ToLower().Contains(f)
                    || (h.notes ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(h => h.card_nr).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<CardHotcardDto?> GetByIdAsync(string cardNr)
        {
            var entity = await _context.CardHotcards.AsNoTracking().FirstOrDefaultAsync(h => h.card_nr == cardNr);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<CardHotcardDto> CreateAsync(CreateCardHotcardRequest request, string actingUser)
        {
            if (await _context.CardHotcards.AsNoTracking().AnyAsync(h => h.card_nr == request.CardNr))
                throw new ConflictException($"Hotcard '{request.CardNr}' already exists.");

            var entity = new SwHotcard
            {
                card_nr = request.CardNr,
                resp_code = request.RespCode,
                auth_id_resp = request.AuthIdResp,
                notes = request.Notes
            };

            _context.CardHotcards.Add(entity);
            _audit.LogInsert(entity, "sw_hotcard", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<CardHotcardDto> UpdateAsync(string cardNr, UpdateCardHotcardRequest request, string actingUser)
        {
            var entity = await _context.CardHotcards.FirstOrDefaultAsync(h => h.card_nr == cardNr)
                ?? throw new NotFoundException($"Hotcard '{cardNr}' not found.");

            var before = new { entity.resp_code, entity.auth_id_resp, entity.notes };
            entity.resp_code = request.RespCode;
            entity.auth_id_resp = request.AuthIdResp;
            entity.notes = request.Notes;

            _audit.LogUpdate(before, new { entity.resp_code, entity.auth_id_resp, entity.notes }, "sw_hotcard", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(string cardNr, string actingUser)
        {
            var entity = await _context.CardHotcards.FirstOrDefaultAsync(h => h.card_nr == cardNr)
                ?? throw new NotFoundException($"Hotcard '{cardNr}' not found.");

            _context.CardHotcards.Remove(entity);
            _audit.LogDelete(new { entity.card_nr }, "sw_hotcard", actingUser);

            await _context.SaveChangesAsync();
        }

        private static CardHotcardDto ToDto(SwHotcard e) => new(e.card_nr, e.resp_code, e.auth_id_resp, e.notes);
    }
}
