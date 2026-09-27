namespace SyncNetApi.Services.Monitoring
{
    /// <summary>Resolves host/port for an Application or Node from the DB (never trusts a
    /// client-supplied host/port — same defense-in-depth spirit as the path-safety checks in
    /// MonitoringLogService) and sends an allow-listed command over TCP via
    /// ISwitchCommandClient. Ports legacy Application/Detail.razor and Nodes/Detail.razor
    /// "Send Command" forms — implemented per explicit user request (2026-09-09), after being
    /// scoped out of Monitoring-1 planning. Deliberately does NOT include Service Start/Stop/
    /// Reset or the bulk "Resync Config" page — those were not part of this request.</summary>
    public interface IMonitoringCommandService
    {
        Task<string> SendApplicationCommandAsync(string appName, string command);
        Task<string> SendInterfaceCommandAsync(int nodeId, string command, string? otherCommand);
    }
}
