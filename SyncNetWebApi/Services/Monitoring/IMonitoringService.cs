using SyncNetApi.Dtos.Monitoring;

namespace SyncNetApi.Services.Monitoring
{
    /// <summary>Read-only projections backing the Monitoring &gt; Realtime pages. All data comes
    /// from tables that already have full CRUD elsewhere in the app (sw_app, sw_nodes,
    /// sw_connections, sw_terminal) — Monitoring never writes to them, it only re-reads with a
    /// lighter [Authorize] gate (see MonitoringController) so non-admin roles granted this menu
    /// in dashboard_role_menu can actually use it.</summary>
    public interface IMonitoringService
    {
        Task<IReadOnlyList<ApplicationMonitorDto>> GetApplicationsAsync(string? filter = null);
        Task<IReadOnlyList<InterfaceMonitorDto>> GetInterfacesAsync(string? filter = null);
        Task<IReadOnlyList<ConnectionMonitorDto>> GetConnectionsAsync(string? filter = null);
        Task<IReadOnlyList<TerminalMonitorDto>> GetTerminalsAsync(string? filter = null);
        Task<IReadOnlyList<ServiceMonitorDto>> GetServicesAsync(string? filter = null);
        Task<IReadOnlyList<JobMonitorDto>> GetJobsAsync(string? filter = null);
        Task<IReadOnlyList<JobLogMonitorDto>> GetJobLogsAsync(string? filter = null);
    }
}
