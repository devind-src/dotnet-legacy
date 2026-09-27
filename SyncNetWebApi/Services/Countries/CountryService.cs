using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.Countries;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.Countries
{
    public class CountryService : ICountryService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public CountryService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<CountryDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.Countries.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(c => c.name.ToLower().Contains(f)
                    || (c.code_alpha_2 ?? "").ToLower().Contains(f)
                    || (c.code_alpha_3 ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(c => c.name).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<CountryDto?> GetByIdAsync(string name)
        {
            var entity = await _context.Countries.AsNoTracking().FirstOrDefaultAsync(c => c.name == name);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<CountryDto> CreateAsync(CreateCountryRequest request, string actingUser)
        {
            var exists = await _context.Countries.AsNoTracking().AnyAsync(c => c.name == request.Name);
            if (exists)
                throw new ConflictException($"Country '{request.Name}' already exists.");

            var entity = new SwCountry
            {
                name = request.Name,
                code_alpha_2 = request.CodeAlpha2,
                code_alpha_3 = request.CodeAlpha3,
                code_numeric = request.CodeNumeric,
                status = "1",
                created_by = actingUser,
                created_dt = DateTime.UtcNow
            };

            _context.Countries.Add(entity);
            _audit.LogInsert(entity, "sw_countries", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<CountryDto> UpdateAsync(string name, UpdateCountryRequest request, string actingUser)
        {
            var entity = await _context.Countries.FirstOrDefaultAsync(c => c.name == name)
                ?? throw new NotFoundException($"Country '{name}' not found.");

            var before = new { entity.code_alpha_2, entity.code_alpha_3, entity.code_numeric, entity.status };
            entity.code_alpha_2 = request.CodeAlpha2;
            entity.code_alpha_3 = request.CodeAlpha3;
            entity.code_numeric = request.CodeNumeric;
            entity.status = request.Active ? "1" : "0";
            entity.updated_by = actingUser;
            entity.updated_dt = DateTime.UtcNow;

            _audit.LogUpdate(before, new { entity.code_alpha_2, entity.code_alpha_3, entity.code_numeric, entity.status }, "sw_countries", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(string name, string actingUser)
        {
            var entity = await _context.Countries.FirstOrDefaultAsync(c => c.name == name)
                ?? throw new NotFoundException($"Country '{name}' not found.");

            _context.Countries.Remove(entity);
            _audit.LogDelete(new { entity.name }, "sw_countries", actingUser);

            await _context.SaveChangesAsync();
        }

        private static CountryDto ToDto(SwCountry e) => new(e.name, e.code_alpha_2, e.code_alpha_3, e.code_numeric, e.status == "1");
    }
}
