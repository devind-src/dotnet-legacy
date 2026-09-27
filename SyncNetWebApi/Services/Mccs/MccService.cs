using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.Mccs;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.Mccs
{
    public class MccService : IMccService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public MccService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<MccDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.MccCodes.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(m => m.mcc_code.ToLower().Contains(f) || (m.mcc_desc ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(m => m.mcc_code).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<MccDto?> GetByIdAsync(string mccCode)
        {
            var entity = await _context.MccCodes.AsNoTracking().FirstOrDefaultAsync(m => m.mcc_code == mccCode);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<MccDto> CreateAsync(CreateMccRequest request, string actingUser)
        {
            var exists = await _context.MccCodes.AsNoTracking().AnyAsync(m => m.mcc_code == request.MccCode);
            if (exists)
                throw new ConflictException($"MCC '{request.MccCode}' already exists.");

            var entity = new SwMcc
            {
                mcc_code = request.MccCode,
                mcc_desc = request.MccDesc,
                floor_limit = request.FloorLimit,
                currency = request.Currency,
                status = "1",
                created_by = actingUser,
                created_dt = DateTime.UtcNow
            };

            _context.MccCodes.Add(entity);
            _audit.LogInsert(entity, "sw_mcc", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<MccDto> UpdateAsync(string mccCode, UpdateMccRequest request, string actingUser)
        {
            var entity = await _context.MccCodes.FirstOrDefaultAsync(m => m.mcc_code == mccCode)
                ?? throw new NotFoundException($"MCC '{mccCode}' not found.");

            var before = new { entity.mcc_desc, entity.floor_limit, entity.currency, entity.status };
            entity.mcc_desc = request.MccDesc;
            entity.floor_limit = request.FloorLimit;
            entity.currency = request.Currency;
            entity.status = request.Active ? "1" : "0";
            entity.updated_by = actingUser;
            entity.updated_dt = DateTime.UtcNow;

            _audit.LogUpdate(before, new { entity.mcc_desc, entity.floor_limit, entity.currency, entity.status }, "sw_mcc", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(string mccCode, string actingUser)
        {
            var entity = await _context.MccCodes.FirstOrDefaultAsync(m => m.mcc_code == mccCode)
                ?? throw new NotFoundException($"MCC '{mccCode}' not found.");

            _context.MccCodes.Remove(entity);
            _audit.LogDelete(new { entity.mcc_code }, "sw_mcc", actingUser);

            await _context.SaveChangesAsync();
        }

        private static MccDto ToDto(SwMcc e) => new(e.mcc_code, e.mcc_desc, e.floor_limit, e.currency, e.status == "1");
    }
}
