using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.Nodes;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.Nodes
{
    public class NodeService : INodeService
    {
        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public NodeService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<NodeDto>> GetRecordsAsync(string? filter = null)
        {
            var query =
                from n in _context.Nodes.AsNoTracking()
                join k in _context.CryptoKeys.AsNoTracking() on n.node_id equals k.node_id into keys
                from k in keys.DefaultIfEmpty()
                join t in _context.SupportTeams.AsNoTracking() on n.team_id equals t.team_id into teams
                from t in teams.DefaultIfEmpty()
                select new { Node = n, Key = k, TeamName = t == null ? null : t.name };

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Sanitize().ToLower();
                query = query.Where(x => x.Node.node_name.ToLower().Contains(f)
                    || (x.Node.app_name ?? "").ToLower().Contains(f)
                    || (x.Node.inst_id ?? "").ToLower().Contains(f));
            }

            var list = await query.OrderBy(x => x.Node.node_name).ToListAsync();
            return list.Select(x => ToDto(x.Node, x.Key, x.TeamName)).ToList();
        }

        public async Task<NodeDto?> GetByIdAsync(int nodeId)
        {
            var node = await _context.Nodes.AsNoTracking().FirstOrDefaultAsync(n => n.node_id == nodeId);
            if (node == null) return null;

            var key = await _context.CryptoKeys.AsNoTracking().FirstOrDefaultAsync(k => k.node_id == nodeId);
            var teamName = node.team_id == null
                ? null
                : await _context.SupportTeams.AsNoTracking().Where(t => t.team_id == node.team_id).Select(t => t.name).FirstOrDefaultAsync();

            return ToDto(node, key, teamName);
        }

        public async Task<(string PortIn, string PortOut)> GetNewPortsAsync()
        {
            var portIn = await _context.Nodes.AsNoTracking().OrderByDescending(n => n.port_in).Select(n => n.port_in).FirstOrDefaultAsync();
            var portOut = await _context.Nodes.AsNoTracking().OrderByDescending(n => n.port_out).Select(n => n.port_out).FirstOrDefaultAsync();

            var nextIn = string.IsNullOrEmpty(portIn) ? 41000 : int.Parse(portIn) + 1;
            var nextOut = string.IsNullOrEmpty(portOut) ? 42000 : int.Parse(portOut) + 1;
            return (nextIn.ToString(), nextOut.ToString());
        }

        public async Task<NodeDto> CreateAsync(CreateNodeRequest request, string actingUser)
        {
            var exists = await _context.Nodes.AsNoTracking().AnyAsync(n => n.node_name == request.NodeName);
            if (exists)
                throw new ConflictException($"Node '{request.NodeName}' already exists.");

            if (!await _context.Apps.AsNoTracking().AnyAsync(a => a.app_name == request.AppName))
                throw new ValidationException($"Application '{request.AppName}' does not exist.");

            if (!await _context.BusinessDates.AsNoTracking().AnyAsync(b => b.business_calendar == request.BusinessCalendar))
                throw new ValidationException($"Business calendar '{request.BusinessCalendar}' does not exist.");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Ports auto-assigned, matching legacy NodeGetNewPortIn/Out.
                var (portIn, portOut) = await GetNewPortsAsync();

                var node = new SwNodes
                {
                    node_name = request.NodeName,
                    app_name = request.AppName,
                    pro_mgr = "Processing Manager",
                    port_in = portIn,
                    port_out = portOut,
                    parameter = request.Parameter,
                    inst_id = request.InstId,
                    auto_signon = request.AutoSignon ? "1" : "0",
                    auto_reversal = request.AutoReversal,
                    auto_reply_reversal = request.AutoReplyReversal ? "1" : "0",
                    save_repeat_reversal = request.SaveRepeatReversal ? "1" : "0",
                    send_cutover_msg = "1",
                    keychange_timer = request.KeychangeTimer,
                    echo_timer = request.EchoTimer,
                    request_timeout = request.RequestTimeout,
                    advice_timeout = request.AdviceTimeout,
                    pin_translate = request.PinTranslate ? "1" : "0",
                    business_calendar = request.BusinessCalendar,
                    security_profile = "",
                    fds_profile = "",
                    remote = 0,
                    saf_limit = request.SafLimit,
                    provider_service = "0",
                    auth_service = "0",
                    issuer = "",
                    limit_class = "",
                    auth_resp = "",
                    team_id = request.TeamId,
                    sensitive_data = request.SensitiveData ? 1 : 0,
                    allocate_trace = "0",
                    category = request.Category,
                    status = "1",
                    created_by = actingUser,
                    created_dt = DateTime.UtcNow
                };
                _context.Nodes.Add(node);
                await _context.SaveChangesAsync();

                var key = new SwCryptoKey
                {
                    node_id = node.node_id,
                    key_length = request.KeyLength,
                    key_type = "1",
                    pinblock_format = request.PinblockFormat,
                    master_key = string.IsNullOrEmpty(request.MasterKey) ? "".PadLeft(32, '0') : request.MasterKey,
                    master_kcv = string.IsNullOrEmpty(request.MasterKcv) ? "".PadLeft(6, '0') : request.MasterKcv,
                    key_under_lmk = string.IsNullOrEmpty(request.KeyUnderLmk) ? "".PadLeft(32, '0') : request.KeyUnderLmk,
                    key_under_zmk = string.IsNullOrEmpty(request.KeyUnderZmk) ? "".PadLeft(32, '0') : request.KeyUnderZmk,
                    key_check_value = string.IsNullOrEmpty(request.KeyCheckValue) ? "".PadLeft(6, '0') : request.KeyCheckValue,
                    status = "1",
                    created_by = actingUser,
                    created_dt = DateTime.UtcNow
                };
                _context.CryptoKeys.Add(key);

                _audit.LogInsert(node, "sw_nodes", actingUser);
                _audit.LogInsert(key, "sw_crypto_keys", actingUser);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var teamName = request.TeamId == null
                    ? null
                    : await _context.SupportTeams.AsNoTracking().Where(t => t.team_id == request.TeamId).Select(t => t.name).FirstOrDefaultAsync();
                return ToDto(node, key, teamName);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<NodeDto> UpdateAsync(int nodeId, UpdateNodeRequest request, string actingUser)
        {
            var node = await _context.Nodes.FirstOrDefaultAsync(n => n.node_id == nodeId)
                ?? throw new NotFoundException($"Node '{nodeId}' not found.");

            if (!await _context.BusinessDates.AsNoTracking().AnyAsync(b => b.business_calendar == request.BusinessCalendar))
                throw new ValidationException($"Business calendar '{request.BusinessCalendar}' does not exist.");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var beforeNode = new
                {
                    node.business_calendar,
                    node.parameter,
                    node.team_id,
                    node.category,
                    node.auto_signon,
                    node.auto_reversal,
                    node.auto_reply_reversal,
                    node.request_timeout,
                    node.advice_timeout,
                    node.keychange_timer,
                    node.echo_timer,
                    node.saf_limit,
                    node.pin_translate,
                    node.sensitive_data,
                    node.save_repeat_reversal
                };

                // port_in/port_out/status/last_connected/etc are deliberately untouched here (see entity note).
                node.business_calendar = request.BusinessCalendar;
                node.parameter = request.Parameter;
                node.team_id = request.TeamId;
                node.category = request.Category;
                node.auto_signon = request.AutoSignon ? "1" : "0";
                node.auto_reversal = request.AutoReversal;
                node.auto_reply_reversal = request.AutoReplyReversal ? "1" : "0";
                node.request_timeout = request.RequestTimeout;
                node.advice_timeout = request.AdviceTimeout;
                node.keychange_timer = request.KeychangeTimer;
                node.echo_timer = request.EchoTimer;
                node.saf_limit = request.SafLimit;
                node.pin_translate = request.PinTranslate ? "1" : "0";
                node.sensitive_data = request.SensitiveData ? 1 : 0;
                node.save_repeat_reversal = request.SaveRepeatReversal ? "1" : "0";
                node.updated_by = actingUser;
                node.updated_dt = DateTime.UtcNow;

                _audit.LogUpdate(beforeNode, new
                {
                    node.business_calendar,
                    node.parameter,
                    node.team_id,
                    node.category,
                    node.auto_signon,
                    node.auto_reversal,
                    node.auto_reply_reversal,
                    node.request_timeout,
                    node.advice_timeout,
                    node.keychange_timer,
                    node.echo_timer,
                    node.saf_limit,
                    node.pin_translate,
                    node.sensitive_data,
                    node.save_repeat_reversal
                }, "sw_nodes", actingUser);

                await _context.SaveChangesAsync();

                var key = await _context.CryptoKeys.FirstOrDefaultAsync(k => k.node_id == nodeId);
                if (key != null)
                {
                    var beforeKey = new { key.key_length, key.pinblock_format, key.master_key, key.master_kcv, key.key_under_lmk, key.key_under_zmk, key.key_check_value };

                    key.key_length = request.KeyLength;
                    key.pinblock_format = request.PinblockFormat;
                    key.master_key = string.IsNullOrEmpty(request.MasterKey) ? key.master_key : request.MasterKey;
                    key.master_kcv = string.IsNullOrEmpty(request.MasterKcv) ? key.master_kcv : request.MasterKcv;
                    key.key_under_lmk = string.IsNullOrEmpty(request.KeyUnderLmk) ? key.key_under_lmk : request.KeyUnderLmk;
                    key.key_under_zmk = string.IsNullOrEmpty(request.KeyUnderZmk) ? key.key_under_zmk : request.KeyUnderZmk;
                    key.key_check_value = string.IsNullOrEmpty(request.KeyCheckValue) ? key.key_check_value : request.KeyCheckValue;
                    key.updated_by = actingUser;
                    key.updated_dt = DateTime.UtcNow;

                    _audit.LogUpdate(beforeKey, new { key.key_length, key.pinblock_format, key.master_key, key.master_kcv, key.key_under_lmk, key.key_under_zmk, key.key_check_value }, "sw_crypto_keys", actingUser);

                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                var teamName = node.team_id == null
                    ? null
                    : await _context.SupportTeams.AsNoTracking().Where(t => t.team_id == node.team_id).Select(t => t.name).FirstOrDefaultAsync();
                return ToDto(node, key, teamName);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteAsync(int nodeId, string actingUser)
        {
            var node = await _context.Nodes.FirstOrDefaultAsync(n => n.node_id == nodeId)
                ?? throw new NotFoundException($"Node '{nodeId}' not found.");

            await EnsureNotReferencedAsync(node.node_id, node.node_name);

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var key = await _context.CryptoKeys.FirstOrDefaultAsync(k => k.node_id == nodeId);
                if (key != null)
                {
                    _context.CryptoKeys.Remove(key);
                    _audit.LogDelete(new { key.id, key.node_id }, "sw_crypto_keys", actingUser);
                    await _context.SaveChangesAsync();
                }

                _context.Nodes.Remove(node);
                _audit.LogDelete(new { node.node_id, node.node_name }, "sw_nodes", actingUser);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // node yang masih dipakai tidak boleh dihapus; sebutkan menu yang masih terhubung (keputusan R4
        // Fase 3 routing). Tabel routing bill payment/topup tidak punya FK ke sw_nodes, jadi dicek di sini.
        private async Task EnsureNotReferencedAsync(int nodeId, string nodeName)
        {
            var used = new List<string>();

            if (await _context.Connections.AsNoTracking().AnyAsync(c => c.node_id == nodeId))
                used.Add("Configuration > Interface > Connections");
            if (await _context.RoutesByInst.AsNoTracking().AnyAsync(r => r.node_id == nodeId)
                || await _context.RoutesByInstAlt.AsNoTracking().AnyAsync(r => r.node_id == nodeId))
                used.Add("Product > Pricing & Fees > Product Fees (biller produk)");
            if (await _context.MarginSuppliers.AsNoTracking().AnyAsync(s => s.supplier_id == nodeName))
                used.Add("Product > Pricing & Fees > Supplier Prices");
            if (await _context.RoutesSchedule.AsNoTracking().AnyAsync(s => s.node_id == nodeId))
                used.Add("Routing > Jadwal Routing");

            if (used.Count > 0)
                throw new ValidationException($"Node '{nodeName}' tidak bisa dihapus karena masih terhubung ke: {string.Join("; ", used)}.");
        }

        private static NodeDto ToDto(SwNodes n, SwCryptoKey? k, string? teamName) => new(
            n.node_id, n.node_name, n.app_name, n.business_calendar, n.inst_id, n.parameter,
            n.port_in, n.port_out, n.team_id, teamName, n.category,
            n.auto_signon == "1", n.auto_reversal, n.auto_reply_reversal == "1",
            n.request_timeout, n.advice_timeout, n.keychange_timer, n.echo_timer, n.saf_limit,
            n.pin_translate == "1", n.sensitive_data == 1, n.save_repeat_reversal == "1",
            n.last_connected, n.last_disconnected, n.last_echo,
            k?.key_length, k?.pinblock_format, k?.master_key, k?.master_kcv, k?.key_under_lmk, k?.key_under_zmk, k?.key_check_value);
    }
}
