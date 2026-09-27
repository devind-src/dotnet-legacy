using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Monitoring
{
    /// <summary>Command must be one of VERSION/ECHO/SIGNON/SIGNOFF/KEYCHANGE/OTHER — validated
    /// server-side too. OtherCommand is required (and only used) when Command == "OTHER",
    /// max length 30 mirrors legacy `maxlength="30"`.</summary>
    public class SendNodeCommandRequest
    {
        [Required]
        public string Command { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? OtherCommand { get; set; }
    }
}
