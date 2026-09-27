using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.TranNames;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.TranNames
{
    public class TranNameService : ITranNameService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public TranNameService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<TranNameDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.TranNames.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(t => t.trans_code.ToLower().Contains(f) || (t.trans_name ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(t => t.trans_code).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<TranNameDto?> GetByIdAsync(string transCode)
        {
            var entity = await _context.TranNames.AsNoTracking().FirstOrDefaultAsync(t => t.trans_code == transCode);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<TranNameDto> CreateAsync(CreateTranNameRequest request, string actingUser)
        {
            var exists = await _context.TranNames.AsNoTracking().AnyAsync(t => t.trans_code == request.TransCode);
            if (exists)
                throw new ConflictException($"Tran name '{request.TransCode}' already exists.");

            var entity = new SwTranName
            {
                trans_code = request.TransCode,
                trans_name = request.TransName,
                status = "1",
                created_by = actingUser,
                created_dt = DateTime.UtcNow
            };

            _context.TranNames.Add(entity);
            _audit.LogInsert(entity, "sw_tran_names", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<TranNameDto> UpdateAsync(string transCode, UpdateTranNameRequest request, string actingUser)
        {
            var entity = await _context.TranNames.FirstOrDefaultAsync(t => t.trans_code == transCode)
                ?? throw new NotFoundException($"Tran name '{transCode}' not found.");

            var before = new { entity.trans_name, entity.status };
            entity.trans_name = request.TransName;
            entity.status = request.Active ? "1" : "0";
            entity.updated_by = actingUser;
            entity.updated_dt = DateTime.UtcNow;

            _audit.LogUpdate(before, new { entity.trans_name, entity.status }, "sw_tran_names", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(string transCode, string actingUser)
        {
            var entity = await _context.TranNames.FirstOrDefaultAsync(t => t.trans_code == transCode)
                ?? throw new NotFoundException($"Tran name '{transCode}' not found.");

            _context.TranNames.Remove(entity);
            _audit.LogDelete(new { entity.trans_code }, "sw_tran_names", actingUser);

            await _context.SaveChangesAsync();
        }

        private static TranNameDto ToDto(SwTranName e) => new(e.trans_code, e.trans_name, e.status == "1");
    }
}
