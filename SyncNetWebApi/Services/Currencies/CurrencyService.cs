using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.Currencies;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.Currencies
{
    public class CurrencyService : ICurrencyService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public CurrencyService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<CurrencyDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.Currencies.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(c => c.currency_code.ToLower().Contains(f)
                    || (c.alpha_code ?? "").ToLower().Contains(f)
                    || (c.name ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(c => c.currency_code).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<CurrencyDto?> GetByIdAsync(string currencyCode)
        {
            var entity = await _context.Currencies.AsNoTracking().FirstOrDefaultAsync(c => c.currency_code == currencyCode);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<CurrencyDto> CreateAsync(CreateCurrencyRequest request, string actingUser)
        {
            var exists = await _context.Currencies.AsNoTracking().AnyAsync(c => c.currency_code == request.CurrencyCode);
            if (exists)
                throw new ConflictException($"Currency '{request.CurrencyCode}' already exists.");

            var entity = new SwCurrency
            {
                currency_code = request.CurrencyCode,
                alpha_code = request.AlphaCode,
                name = request.Name,
                nr_decimals = request.NrDecimals,
                rate = request.Rate,
                status = "1",
                created_by = actingUser,
                created_dt = DateTime.UtcNow
            };

            _context.Currencies.Add(entity);
            _audit.LogInsert(entity, "sw_currencies", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<CurrencyDto> UpdateAsync(string currencyCode, UpdateCurrencyRequest request, string actingUser)
        {
            var entity = await _context.Currencies.FirstOrDefaultAsync(c => c.currency_code == currencyCode)
                ?? throw new NotFoundException($"Currency '{currencyCode}' not found.");

            var before = new { entity.alpha_code, entity.name, entity.nr_decimals, entity.rate, entity.status };
            entity.alpha_code = request.AlphaCode;
            entity.name = request.Name;
            entity.nr_decimals = request.NrDecimals;
            entity.rate = request.Rate;
            entity.status = request.Active ? "1" : "0";
            entity.updated_by = actingUser;
            entity.updated_dt = DateTime.UtcNow;

            _audit.LogUpdate(before, new { entity.alpha_code, entity.name, entity.nr_decimals, entity.rate, entity.status }, "sw_currencies", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(string currencyCode, string actingUser)
        {
            var entity = await _context.Currencies.FirstOrDefaultAsync(c => c.currency_code == currencyCode)
                ?? throw new NotFoundException($"Currency '{currencyCode}' not found.");

            _context.Currencies.Remove(entity);
            _audit.LogDelete(new { entity.currency_code }, "sw_currencies", actingUser);

            await _context.SaveChangesAsync();
        }

        private static CurrencyDto ToDto(SwCurrency e) => new(e.currency_code, e.alpha_code, e.name, e.nr_decimals, e.rate, e.status == "1");
    }
}
