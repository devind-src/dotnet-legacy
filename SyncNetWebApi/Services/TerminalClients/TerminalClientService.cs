using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.TerminalClients;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.TerminalClients
{
    public class TerminalClientService : ITerminalClientService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public TerminalClientService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<TerminalClientDto>> GetRecordsAsync(string? filter = null)
        {
            var query = _context.TerminalClients.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(c => c.client_id.ToLower().Contains(f) || c.client_name.ToLower().Contains(f));
            }

            var list = await query.OrderBy(c => c.client_id).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<TerminalClientDto?> GetByIdAsync(string clientId)
        {
            var entity = await _context.TerminalClients.AsNoTracking().FirstOrDefaultAsync(c => c.client_id == clientId);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<TerminalClientDto> CreateAsync(CreateTerminalClientRequest request, string actingUser)
        {
            var exists = await _context.TerminalClients.AsNoTracking().AnyAsync(c => c.client_id == request.ClientId);
            if (exists)
                throw new ConflictException($"Credential '{request.ClientId}' already exists.");

            var entity = new SwTerminalClient
            {
                client_id = request.ClientId,
                client_name = request.ClientName,
                username = request.Username,
                password = request.Password,
                secret_id = request.SecretId,
                secret_key = request.SecretKey,
                master_key = request.MasterKey,
                session_key = request.SessionKey,
                group_name = request.GroupName,
                subgroup_name = request.SubgroupName,
                callback_url = request.CallbackUrl,
                callback_client_id = request.CallbackClientId,
                callback_secret_key = request.CallbackSecretKey
            };

            _context.TerminalClients.Add(entity);
            _audit.LogInsert(entity, "sw_terminal_client", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<TerminalClientDto> UpdateAsync(string clientId, UpdateTerminalClientRequest request, string actingUser)
        {
            var entity = await _context.TerminalClients.FirstOrDefaultAsync(c => c.client_id == clientId)
                ?? throw new NotFoundException($"Credential '{clientId}' not found.");

            var before = ToAuditSnapshot(entity);

            entity.client_name = request.ClientName;
            entity.username = request.Username;
            entity.password = request.Password;
            entity.secret_id = request.SecretId;
            entity.secret_key = request.SecretKey;
            entity.master_key = request.MasterKey;
            entity.session_key = request.SessionKey;
            entity.group_name = request.GroupName;
            entity.subgroup_name = request.SubgroupName;
            entity.callback_url = request.CallbackUrl;
            entity.callback_client_id = request.CallbackClientId;
            entity.callback_secret_key = request.CallbackSecretKey;

            _audit.LogUpdate(before, ToAuditSnapshot(entity), "sw_terminal_client", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(string clientId, string actingUser)
        {
            var entity = await _context.TerminalClients.FirstOrDefaultAsync(c => c.client_id == clientId)
                ?? throw new NotFoundException($"Credential '{clientId}' not found.");

            _context.TerminalClients.Remove(entity);
            _audit.LogDelete(new { entity.client_id, entity.client_name }, "sw_terminal_client", actingUser);

            await _context.SaveChangesAsync();
        }

        private static object ToAuditSnapshot(SwTerminalClient e) => new
        {
            e.client_name, e.username, e.group_name, e.subgroup_name, e.callback_url, e.callback_client_id
        };

        private static TerminalClientDto ToDto(SwTerminalClient e) => new(
            e.client_id, e.client_name, e.username, e.password, e.secret_id, e.secret_key,
            e.master_key, e.session_key, e.group_name, e.subgroup_name, e.callback_url,
            e.callback_client_id, e.callback_secret_key);
    }
}
