using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Connections
{
    /// <summary>ConnName/NodeId are immutable after create — matches legacy UI, which renders
    /// Connection Name readonly on edit and never exposes a way to move a connection to a
    /// different node.</summary>
    public class UpdateConnectionRequest
    {
        [Required, RegularExpression("^[0-8]$")]
        public string Protocol { get; set; } = "0";

        [RegularExpression("^[01]$")]
        public string TcpHeaderFormat { get; set; } = "0";

        [MaxLength(2)]
        public string? TcpFooter { get; set; }

        public bool TcpHiLo { get; set; } = true;

        [RegularExpression("^[01]$")]
        public string ConnType { get; set; } = "0";

        [MaxLength(16)]
        public string? IpAddress { get; set; }

        [MaxLength(5)]
        public string? Port { get; set; }

        public int MaxConn { get; set; } = 65;

        public int RetryDelay { get; set; }

        public bool AlwaysConnected { get; set; } = true;

        public bool OneSocketOnly { get; set; } = true;

        [MaxLength(100)]
        public string? QueueInbox { get; set; }

        [MaxLength(100)]
        public string? QueueOutbox { get; set; }

        [MaxLength(100)]
        public string? WsUrl { get; set; }

        [MaxLength(15)]
        public string? WsMethod { get; set; } = "POST";

        [MaxLength(100)]
        public string? WsContent { get; set; } = "application/json";
    }
}
