namespace SyncNetApi.Options
{
    /// <summary>Where Monitoring &gt; Loggers (Log Viewer / Trace Viewer) reads log/trace files
    /// from on disk. Mirrors legacy appsettings.json "Paths:Windows:Logs"/"Paths:Windows:Traces"
    /// — same physical folders as FileLoggingOptions' default (C:\SyncNet\Logs), confirmed live
    /// on this machine to already hold real per-application log/trace files written by the
    /// legacy switching engine that is still running under C:\SyncNet (see
    /// PROJECT_TECHNICAL_SUMMARY.md §7.11.1/§7.21/§7.23). Read-only — Monitoring never writes
    /// here.</summary>
    public class MonitoringOptions
    {
        public const string SectionName = "Monitoring";

        public string LogDir { get; set; } = @"C:\SyncNet\Logs";

        public string TraceDir { get; set; } = @"C:\SyncNet\Traces";

        /// <summary>Max lines returned per content read (Log Viewer and Trace Viewer both page
        /// through files rather than reading a whole file into memory/response — deliberate
        /// departure from legacy Log Viewer's File.ReadAllText, confirmed with user before
        /// building since production log files under this shared folder can be large).</summary>
        public int PageSize { get; set; } = 200;
    }
}
