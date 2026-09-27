using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Apps
{
    /// <summary>AppName is immutable after create — matches legacy UI, which renders it
    /// readonly on edit. Unlike create, legacy does NOT re-check command_port uniqueness on
    /// update (replicated as-is, not "fixed").</summary>
    public class UpdateAppRequest
    {
        [Required, RegularExpression("^[0-8]$")]
        public string AppType { get; set; } = "1";

        [Required, MaxLength(30)]
        public string Host { get; set; } = "localhost";

        [Required, MaxLength(5)]
        public string CommandPort { get; set; } = string.Empty;

        public bool Active { get; set; } = true;
    }
}
