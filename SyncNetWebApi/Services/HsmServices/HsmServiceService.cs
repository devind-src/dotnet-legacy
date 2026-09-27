using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.HsmServices;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.HsmServices
{
    public class HsmServiceService : IHsmServiceService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public HsmServiceService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<HsmServiceDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.HsmServices.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(s => s.hsm_desc.ToLower().Contains(f));
            }

            var list = await query.OrderBy(s => s.hsm_desc).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<HsmServiceDto?> GetByIdAsync(string hsmDesc)
        {
            var entity = await _context.HsmServices.AsNoTracking().FirstOrDefaultAsync(s => s.hsm_desc == hsmDesc);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<HsmServiceDto> CreateAsync(CreateHsmServiceRequest request, string actingUser)
        {
            var exists = await _context.HsmServices.AsNoTracking().AnyAsync(s => s.hsm_desc == request.HsmDesc);
            if (exists)
                throw new ConflictException($"HSM service '{request.HsmDesc}' already exists.");

            var entity = new SwCryptoService
            {
                hsm_desc = request.HsmDesc,
                hsm_port = request.HsmPort,
                request_timeout = request.RequestTimeout,
                message_header = request.MessageHeader,
                created_by = actingUser,
                created_dt = DateTime.UtcNow
            };

            _context.HsmServices.Add(entity);
            _audit.LogInsert(entity, "sw_crypto_service", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<HsmServiceDto> UpdateAsync(string hsmDesc, UpdateHsmServiceRequest request, string actingUser)
        {
            var entity = await _context.HsmServices.FirstOrDefaultAsync(s => s.hsm_desc == hsmDesc)
                ?? throw new NotFoundException($"HSM service '{hsmDesc}' not found.");

            var before = new { entity.hsm_port, entity.request_timeout, entity.message_header };

            entity.hsm_port = request.HsmPort;
            entity.request_timeout = request.RequestTimeout;
            entity.message_header = request.MessageHeader;
            entity.updated_by = actingUser;
            entity.updated_dt = DateTime.UtcNow;

            _audit.LogUpdate(before, new { entity.hsm_port, entity.request_timeout, entity.message_header }, "sw_crypto_service", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(string hsmDesc, string actingUser)
        {
            var entity = await _context.HsmServices.FirstOrDefaultAsync(s => s.hsm_desc == hsmDesc)
                ?? throw new NotFoundException($"HSM service '{hsmDesc}' not found.");

            _context.HsmServices.Remove(entity);
            _audit.LogDelete(new { entity.hsm_desc }, "sw_crypto_service", actingUser);

            await _context.SaveChangesAsync();
        }

        private static HsmServiceDto ToDto(SwCryptoService e) => new(e.hsm_desc, e.hsm_port, e.request_timeout, e.message_header);
    }
}
