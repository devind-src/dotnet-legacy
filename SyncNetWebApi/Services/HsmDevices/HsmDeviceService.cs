using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.HsmDevices;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.HsmDevices
{
    public class HsmDeviceService : IHsmDeviceService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public HsmDeviceService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<HsmDeviceDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.HsmDevices.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(d => d.hsm_name.ToLower().Contains(f));
            }

            var list = await query.OrderBy(d => d.hsm_name).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<HsmDeviceDto?> GetByIdAsync(string hsmName)
        {
            var entity = await _context.HsmDevices.AsNoTracking().FirstOrDefaultAsync(d => d.hsm_name == hsmName);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<HsmDeviceDto> CreateAsync(CreateHsmDeviceRequest request, string actingUser)
        {
            var exists = await _context.HsmDevices.AsNoTracking().AnyAsync(d => d.hsm_name == request.HsmName);
            if (exists)
                throw new ConflictException($"HSM device '{request.HsmName}' already exists.");

            var entity = new SwCryptoHsm
            {
                hsm_name = request.HsmName,
                priority = request.Priority,
                protocol = request.Protocol,
                use_scheme = request.UseScheme ? "1" : "0",
                message_header = request.MessageHeader,
                remote_ip = request.Protocol == "1" ? request.RemoteIp : null,
                remote_port = request.Protocol == "1" ? request.RemotePort : null,
                created_by = actingUser,
                created_dt = DateTime.UtcNow
            };

            _context.HsmDevices.Add(entity);
            _audit.LogInsert(entity, "sw_crypto_hsm", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<HsmDeviceDto> UpdateAsync(string hsmName, UpdateHsmDeviceRequest request, string actingUser)
        {
            var entity = await _context.HsmDevices.FirstOrDefaultAsync(d => d.hsm_name == hsmName)
                ?? throw new NotFoundException($"HSM device '{hsmName}' not found.");

            var before = new { entity.priority, entity.protocol, entity.use_scheme, entity.message_header, entity.remote_ip, entity.remote_port };

            entity.priority = request.Priority;
            entity.protocol = request.Protocol;
            entity.use_scheme = request.UseScheme ? "1" : "0";
            entity.message_header = request.MessageHeader;
            entity.remote_ip = request.Protocol == "1" ? request.RemoteIp : null;
            entity.remote_port = request.Protocol == "1" ? request.RemotePort : null;
            entity.updated_by = actingUser;
            entity.updated_dt = DateTime.UtcNow;

            _audit.LogUpdate(before, new { entity.priority, entity.protocol, entity.use_scheme, entity.message_header, entity.remote_ip, entity.remote_port }, "sw_crypto_hsm", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(string hsmName, string actingUser)
        {
            var entity = await _context.HsmDevices.FirstOrDefaultAsync(d => d.hsm_name == hsmName)
                ?? throw new NotFoundException($"HSM device '{hsmName}' not found.");

            _context.HsmDevices.Remove(entity);
            _audit.LogDelete(new { entity.hsm_name }, "sw_crypto_hsm", actingUser);

            await _context.SaveChangesAsync();
        }

        private static HsmDeviceDto ToDto(SwCryptoHsm e) => new(
            e.hsm_name, e.priority, e.protocol, e.use_scheme == "1", e.message_header, e.remote_ip, e.remote_port);
    }
}
