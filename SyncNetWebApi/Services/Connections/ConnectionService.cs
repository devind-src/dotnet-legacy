using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;
using SyncNetApi.Dtos.Connections;
using SyncNetApi.Entities;
using SyncNetApi.Services.Audit;

namespace SyncNetApi.Services.Connections
{
    public class ConnectionService : IConnectionService
    {
        // Protocol families — matches legacy Detail.razor's SetProtocol() switch.
        private static readonly HashSet<string> TcpProtocols = new() { "0", "1", "2", "3", "4", "5" };
        private const string MsQueueProtocol = "6";
        private const string WebServiceProtocol = "7";
        private const string CustomProtocol = "8";

        private readonly SyncNetDbContext _context;
        private readonly IAuditService _audit;

        public ConnectionService(SyncNetDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IReadOnlyList<ConnectionDto>> GetByNodeIdAsync(int nodeId)
        {
            var list = await _context.Connections.AsNoTracking()
                .Where(c => c.node_id == nodeId)
                .OrderBy(c => c.conn_name)
                .ToListAsync();

            return list.Select(ToDto).ToList();
        }

        public async Task<ConnectionDto?> GetByIdAsync(string connName)
        {
            var entity = await _context.Connections.AsNoTracking().FirstOrDefaultAsync(c => c.conn_name == connName);
            return entity == null ? null : ToDto(entity);
        }

        public async Task<ConnectionDto> CreateAsync(CreateConnectionRequest request, string actingUser)
        {
            if (!await _context.Nodes.AsNoTracking().AnyAsync(n => n.node_id == request.NodeId))
                throw new ValidationException($"Node '{request.NodeId}' does not exist.");

            Validate(request.Protocol, request.ConnType, request.IpAddress, request.Port, request.MaxConn,
                request.QueueInbox, request.QueueOutbox, request.WsUrl, request.WsMethod, request.WsContent);

            if (await _context.Connections.AsNoTracking().AnyAsync(c => c.conn_name == request.ConnName))
                throw new ConflictException($"Connection '{request.ConnName}' already exists.");

            var entity = new SwConnection
            {
                conn_name = request.ConnName,
                node_id = request.NodeId,
                protocol = request.Protocol,
                tcp_header_format = request.TcpHeaderFormat,
                tcp_footer = request.TcpFooter ?? "",
                tcp_hi_lo = (short)(request.TcpHiLo ? 1 : 0),
                conn_type = request.ConnType,
                ip_address = request.IpAddress,
                port = request.Port,
                max_conn = request.MaxConn,
                retry_delay = request.RetryDelay,
                always_connected = request.AlwaysConnected ? "1" : "0",
                one_socket_only = request.OneSocketOnly ? "1" : "0",
                queue_inbox = request.QueueInbox,
                queue_outbox = request.QueueOutbox,
                ws_url = request.WsUrl,
                ws_method = request.WsMethod,
                ws_content = request.WsContent,
                status = "1"
            };

            _context.Connections.Add(entity);
            _audit.LogInsert(entity, "sw_connections", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task<ConnectionDto> UpdateAsync(string connName, UpdateConnectionRequest request, string actingUser)
        {
            var entity = await _context.Connections.FirstOrDefaultAsync(c => c.conn_name == connName)
                ?? throw new NotFoundException($"Connection '{connName}' not found.");

            Validate(request.Protocol, request.ConnType, request.IpAddress, request.Port, request.MaxConn,
                request.QueueInbox, request.QueueOutbox, request.WsUrl, request.WsMethod, request.WsContent);

            var before = ToDto(entity);

            entity.protocol = request.Protocol;
            entity.tcp_header_format = request.TcpHeaderFormat;
            entity.tcp_footer = request.TcpFooter ?? "";
            entity.tcp_hi_lo = (short)(request.TcpHiLo ? 1 : 0);
            entity.conn_type = request.ConnType;
            entity.ip_address = request.IpAddress;
            entity.port = request.Port;
            entity.max_conn = request.MaxConn;
            entity.retry_delay = request.RetryDelay;
            entity.always_connected = request.AlwaysConnected ? "1" : "0";
            entity.one_socket_only = request.OneSocketOnly ? "1" : "0";
            entity.queue_inbox = request.QueueInbox;
            entity.queue_outbox = request.QueueOutbox;
            entity.ws_url = request.WsUrl;
            entity.ws_method = request.WsMethod;
            entity.ws_content = request.WsContent;

            _audit.LogUpdate(before, ToDto(entity), "sw_connections", actingUser);

            await _context.SaveChangesAsync();
            return ToDto(entity);
        }

        public async Task DeleteAsync(string connName, string actingUser)
        {
            var entity = await _context.Connections.FirstOrDefaultAsync(c => c.conn_name == connName)
                ?? throw new NotFoundException($"Connection '{connName}' not found.");

            _context.Connections.Remove(entity);
            _audit.LogDelete(entity, "sw_connections", actingUser);

            await _context.SaveChangesAsync();
        }

        // Mirrors legacy Detail.razor's IsValid() — required fields depend on the selected
        // protocol family, so this can't be expressed as static DataAnnotations.
        private static void Validate(string protocol, string connType, string? ipAddress, string? port,
            int maxConn, string? queueInbox, string? queueOutbox, string? wsUrl, string? wsMethod, string? wsContent)
        {
            if (TcpProtocols.Contains(protocol))
            {
                if (string.IsNullOrWhiteSpace(ipAddress))
                    throw new ValidationException("IP address mandatory.");
                if (string.IsNullOrWhiteSpace(port))
                    throw new ValidationException("Port mandatory.");
                if (connType == "0" && maxConn == 0)
                    throw new ValidationException("Max connection mandatory.");
            }
            else if (protocol == MsQueueProtocol)
            {
                if (string.IsNullOrWhiteSpace(queueInbox))
                    throw new ValidationException("Inbox mandatory.");
                if (string.IsNullOrWhiteSpace(queueOutbox))
                    throw new ValidationException("Outbox mandatory.");
            }
            else if (protocol == WebServiceProtocol)
            {
                if (string.IsNullOrWhiteSpace(wsUrl))
                    throw new ValidationException("URL mandatory.");
                if (string.IsNullOrWhiteSpace(wsMethod))
                    throw new ValidationException("Method mandatory.");
                if (string.IsNullOrWhiteSpace(wsContent))
                    throw new ValidationException("Content mandatory.");
            }
            else if (protocol == CustomProtocol)
            {
                if (string.IsNullOrWhiteSpace(wsUrl))
                    throw new ValidationException("File config mandatory.");
            }
        }

        private static ConnectionDto ToDto(SwConnection e) => new(
            e.conn_name, e.node_id, e.protocol, e.tcp_header_format, e.tcp_footer, e.tcp_hi_lo == 1,
            e.conn_type, e.ip_address, e.port, e.max_conn, e.retry_delay, e.always_connected == "1",
            e.one_socket_only == "1", e.queue_inbox, e.queue_outbox, e.ws_url, e.ws_method, e.ws_content,
            e.last_connected, e.last_disconnected);
    }
}
