using Microsoft.Extensions.Logging;

namespace SyncNetApi.Options
{
    /// <summary>Where unhandled/internal-error log entries are written on disk, so a production
    /// deployment can be diagnosed without console access. Bound from the "Logging:File" section
    /// of appsettings.json — change Folder/FileName per environment without recompiling.</summary>
    public class FileLoggingOptions
    {
        public const string SectionName = "Logging:File";

        /// <summary>Absolute folder path the log file is written into. Created at startup if it
        /// does not exist.</summary>
        public string Folder { get; set; } = @"C:\SyncNet\Logs";

        /// <summary>Log file name within Folder.</summary>
        public string FileName { get; set; } = "syncnet-api.log";

        /// <summary>Minimum level written to the file — defaults to Error so the file stays
        /// focused on internal errors, not routine request noise (the console/debug providers
        /// already cover Information-level detail during local development).</summary>
        public LogLevel MinLevel { get; set; } = LogLevel.Error;
    }
}
