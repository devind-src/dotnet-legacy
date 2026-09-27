using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.Cities;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.Cities
{
    public class CityService : ICityService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public CityService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<CityDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.Cities.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(c => (c.city ?? "").ToLower().Contains(f) || (c.province ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(c => c.city).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<CityDto?> GetByIdAsync(long id)
        {
            var entity = await _context.Cities.AsNoTracking().FirstOrDefaultAsync(c => c.id == id);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<CityDto> CreateAsync(CreateCityRequest request, string actingUser)
        {
            // sw_cities.id is GENERATED ALWAYS AS IDENTITY — do not set it, the DB assigns it
            // and EF reads the value back after SaveChangesAsync (see entity note).
            var entity = new SwCity
            {
                city = request.City,
                province = request.Province,
                city_type = request.CityType
            };

            _context.Cities.Add(entity);
            _audit.LogInsert(entity, "sw_cities", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<CityDto> UpdateAsync(long id, UpdateCityRequest request, string actingUser)
        {
            var entity = await _context.Cities.FirstOrDefaultAsync(c => c.id == id)
                ?? throw new NotFoundException($"City '{id}' not found.");

            var before = new { entity.city, entity.province, entity.city_type };
            entity.city = request.City;
            entity.province = request.Province;
            entity.city_type = request.CityType;

            _audit.LogUpdate(before, new { entity.city, entity.province, entity.city_type }, "sw_cities", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(long id, string actingUser)
        {
            var entity = await _context.Cities.FirstOrDefaultAsync(c => c.id == id)
                ?? throw new NotFoundException($"City '{id}' not found.");

            _context.Cities.Remove(entity);
            _audit.LogDelete(new { entity.id, entity.city }, "sw_cities", actingUser);

            await _context.SaveChangesAsync();
        }

        private static CityDto ToDto(SwCity e) => new(e.id, e.city, e.province, e.city_type);
    }
}
