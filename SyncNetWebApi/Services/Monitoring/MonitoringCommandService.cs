using Microsoft.EntityFrameworkCore;
using SyncNetApi.Common;
using SyncNetApi.Data;

namespace SyncNetApi.Services.Monitoring
{
    public class MonitoringCommandService : IMonitoringCommandService
    {
        // Mirrors the option lists in legacy Application/Detail.razor and Nodes/Detail.razor —
        // enforced server-side too, not just via the frontend dropdown.
        private static readonly string[] ApplicationCommands = { "VERSION", "RESYNC", "TRACE ON", "TRACE OFF" };
        private static readonly string[] NodeCommands = { "VERSION", "ECHO", "SIGNON", "SIGNOFF", "KEYCHANGE", "OTHER" };
        private static readonly string[] NodeCommandsWithNodeNameSuffix = { "ECHO", "SIGNON", "SIGNOFF", "KEYCHANGE" };

        private readonly SyncNetDbContext _context;
        private readonly ISwitchCommandClient _client;

        public MonitoringCommandService(SyncNetDbContext context, ISwitchCommandClient client)
        {
            _context = context;
            _client = client;
        }

        public async Task<string> SendApplicationCommandAsync(string appName, string command)
        {
            if (!ApplicationCommands.Contains(command))
                throw new ValidationException($"Unsupported command '{command}'.");

            var app = await _context.Apps.AsNoTracking().FirstOrDefaultAsync(a => a.app_name == appName)
                ?? throw new NotFoundException($"Application '{appName}' not found.");

            var (host, port) = ResolveEndpoint(app.host, app.command_port, appName);

            // Legacy Application/Detail.razor sends the raw dropdown value as-is, no framing.
            return await _client.SendAsync(host, port, command);
        }

        public async Task<string> SendInterfaceCommandAsync(int nodeId, string command, string? otherCommand)
        {
            if (!NodeCommands.Contains(command))
                throw new ValidationException($"Unsupported command '{command}'.");

            if (command == "OTHER" && string.IsNullOrWhiteSpace(otherCommand))
                throw new ValidationException("Other command is required.");

            var node = await _context.Nodes.AsNoTracking().FirstOrDefaultAsync(n => n.node_id == nodeId)
                ?? throw new NotFoundException($"Node '{nodeId}' not found.");

            var app = await _context.Apps.AsNoTracking().FirstOrDefaultAsync(a => a.app_name == node.app_name)
                ?? throw new NotFoundException($"Application '{node.app_name}' for node '{node.node_name}' not found.");

            var (host, port) = ResolveEndpoint(app.host, app.command_port, node.app_name ?? string.Empty);

            // Mirrors legacy Nodes/Detail.razor Send(): VERSION goes out bare, OTHER carries a
            // free-text suffix, the rest get "{command} {node_name}".
            var request = command switch
            {
                "OTHER" => $"OTHER {node.node_name} {otherCommand}",
                _ when NodeCommandsWithNodeNameSuffix.Contains(command) => $"{command} {node.node_name}",
                _ => command
            };

            return await _client.SendAsync(host, port, request);
        }

        private static (string Host, int Port) ResolveEndpoint(string? host, string? commandPort, string appName)
        {
            if (string.IsNullOrWhiteSpace(host) || !int.TryParse(commandPort, out var port))
                throw new ValidationException($"Application '{appName}' does not have a valid host/command port configured.");

            return (host, port);
        }
    }
}
