using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Monitoring
{
    /// <summary>Command must be one of VERSION/RESYNC/TRACE ON/TRACE OFF — validated
    /// server-side too (see MonitoringCommandService), not just via the frontend dropdown.</summary>
    public class SendApplicationCommandRequest
    {
        [Required]
        public string Command { get; set; } = string.Empty;
    }
}
