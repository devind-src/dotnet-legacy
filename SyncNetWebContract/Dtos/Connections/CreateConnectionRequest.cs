using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Connections
{
    /// <summary>Protocol: "0"-"5" TCP/IP variants, "6" Message Queue, "7" Web Service, "8"
    /// Custom (matches legacy Protocol.cs / Detail.razor dropdown). Conditional-required fields
    /// per protocol family (IpAddress/Port for TCP, QueueInbox/QueueOutbox for MSQueue,
    /// WsUrl/WsMethod/WsContent for WebService, WsUrl for Custom) are validated server-side in
    /// ConnectionService — replicates legacy Detail.razor's IsValid(), not expressible as static
    /// attributes since the requirement depends on the selected Protocol/ConnType.</summary>
    public class CreateConnectionRequest
    {
        [Required, MaxLength(20)]
        public string ConnName { get; set; } = string.Empty;

        [Required]
        public int NodeId { get; set; }

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
