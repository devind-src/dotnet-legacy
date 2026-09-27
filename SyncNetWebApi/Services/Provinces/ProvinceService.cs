using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.Provinces;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.Provinces
{
    public class ProvinceService : IProvinceService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public ProvinceService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<ProvinceDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.Provinces.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(p => (p.province ?? "").ToLower().Contains(f) || (p.island ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(p => p.province).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<ProvinceDto?> GetByIdAsync(long id)
        {
            var entity = await _context.Provinces.AsNoTracking().FirstOrDefaultAsync(p => p.id == id);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<ProvinceDto> CreateAsync(CreateProvinceRequest request, string actingUser)
        {
            // sw_province.id is GENERATED ALWAYS AS IDENTITY — do not set it, the DB assigns it
            // and EF reads the value back after SaveChangesAsync (see entity note).
            var entity = new SwProvince
            {
                province = request.Province,
                capital = request.Capital,
                island = request.Island
            };

            _context.Provinces.Add(entity);
            _audit.LogInsert(entity, "sw_province", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<ProvinceDto> UpdateAsync(long id, UpdateProvinceRequest request, string actingUser)
        {
            var entity = await _context.Provinces.FirstOrDefaultAsync(p => p.id == id)
                ?? throw new NotFoundException($"Province '{id}' not found.");

            var before = new { entity.province, entity.capital, entity.island };
            entity.province = request.Province;
            entity.capital = request.Capital;
            entity.island = request.Island;

            _audit.LogUpdate(before, new { entity.province, entity.capital, entity.island }, "sw_province", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(long id, string actingUser)
        {
            var entity = await _context.Provinces.FirstOrDefaultAsync(p => p.id == id)
                ?? throw new NotFoundException($"Province '{id}' not found.");

            _context.Provinces.Remove(entity);
            _audit.LogDelete(new { entity.id, entity.province }, "sw_province", actingUser);

            await _context.SaveChangesAsync();
        }

        private static ProvinceDto ToDto(SwProvince e) => new(e.id, e.province, e.capital, e.island);
    }
}
