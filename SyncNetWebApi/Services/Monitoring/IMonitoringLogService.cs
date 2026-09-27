using SyncNetApi.Dtos.Monitoring;

namespace SyncNetApi.Services.Monitoring
{
    /// <summary>Read-only filesystem browsing for Monitoring &gt; Loggers (Log Viewer / Trace
    /// Viewer, dashboard_menu menu_id 7101/7102). Backs directly onto C:\SyncNet\Logs and
    /// C:\SyncNet\Traces (see MonitoringOptions) — the same folders the still-live legacy
    /// switching engine writes to. Every read is paginated (see MonitoringOptions.PageSize);
    /// nothing here ever loads a whole file into memory, unlike the legacy Log Viewer's
    /// File.ReadAllText (deliberate departure, confirmed with user before building).</summary>
    public interface IMonitoringLogService
    {
        IReadOnlyList<string> GetLogAppNames();
        IReadOnlyList<MonitoringFileDto> GetLogFiles(string appName);
        MonitoringFileContentDto GetLogContent(string fileName, int offset, int pageSize);

        IReadOnlyList<string> GetTraceAppNames();
        IReadOnlyList<MonitoringFileDto> GetTraceFiles(string appName);
        MonitoringFileContentDto GetTraceContent(string appName, string fileName, int offset, int pageSize, string? search);
        (byte[] Content, string DownloadFileName) GetTraceFileZip(string appName, string fileName);
    }
}
