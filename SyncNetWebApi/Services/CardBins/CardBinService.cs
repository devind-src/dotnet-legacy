using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.CardBins;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.CardBins
{
    public class CardBinService : ICardBinService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public CardBinService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<CardBinDto>> GetRecordsAsync(string? filter = null)
        {
            var query = from b in _context.CardBins.AsNoTracking()
                        join g in _context.CardGroups.AsNoTracking() on b.group_id equals g.group_id into gj
                        from g in gj.DefaultIfEmpty()
                        select new { Bin = b, GroupName = g == null ? null : g.group_name };

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(x => x.Bin.bin_nr.ToLower().Contains(f) || (x.GroupName ?? "").ToLower().Contains(f) || (x.Bin.bin_desc ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(x => x.Bin.bin_nr).ToListAsync();
            return list.Select(x => new CardBinDto(x.Bin.bin_nr, x.Bin.group_id, x.GroupName, x.Bin.bin_desc)).ToList();
        }

        public async Task<CardBinDto?> GetByIdAsync(string binNr)
        {
            var entity = await _context.CardBins.AsNoTracking().FirstOrDefaultAsync(b => b.bin_nr == binNr);
            if (entity == null) return null;

            var groupName = await _context.CardGroups.AsNoTracking()
                .Where(g => g.group_id == entity.group_id)
                .Select(g => g.group_name)
                .FirstOrDefaultAsync();

            return new CardBinDto(entity.bin_nr, entity.group_id, groupName, entity.bin_desc);
        }

        public async Task<CardBinDto> CreateAsync(CreateCardBinRequest request, string actingUser)
        {
            if (!await _context.CardGroups.AsNoTracking().AnyAsync(g => g.group_id == request.GroupId))
                throw new ValidationException($"Card group '{request.GroupId}' does not exist.");

            if (await _context.CardBins.AsNoTracking().AnyAsync(b => b.bin_nr == request.BinNr))
                throw new ConflictException($"BIN '{request.BinNr}' already exists.");

            var entity = new SwBin
            {
                bin_nr = request.BinNr,
                group_id = request.GroupId,
                bin_desc = request.BinDesc
            };

            _context.CardBins.Add(entity);
            _audit.LogInsert(entity, "sw_bins", actingUser);

            await _context.SaveChangesAsync();
            return (await GetByIdAsync(entity.bin_nr))!;
        }

        public async Task<CardBinDto> UpdateAsync(string binNr, UpdateCardBinRequest request, string actingUser)
        {
            var entity = await _context.CardBins.FirstOrDefaultAsync(b => b.bin_nr == binNr)
                ?? throw new NotFoundException($"BIN '{binNr}' not found.");

            var before = new { entity.bin_desc };
            entity.bin_desc = request.BinDesc;

            _audit.LogUpdate(before, new { entity.bin_desc }, "sw_bins", actingUser);

            await _context.SaveChangesAsync();
            return (await GetByIdAsync(entity.bin_nr))!;
        }

        public async Task DeleteAsync(string binNr, string actingUser)
        {
            var entity = await _context.CardBins.FirstOrDefaultAsync(b => b.bin_nr == binNr)
                ?? throw new NotFoundException($"BIN '{binNr}' not found.");

            _context.CardBins.Remove(entity);
            _audit.LogDelete(new { entity.bin_nr, entity.group_id }, "sw_bins", actingUser);

            await _context.SaveChangesAsync();
        }
    }
}
