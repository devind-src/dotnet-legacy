namespace SyncNetApi.Dtos.Connections
{
    /// <summary>LastConnected/LastDisconnected are read-only display fields — runtime monitoring
    /// data, never written by this API's Create/Update.</summary>
    public record ConnectionDto(
        string ConnName,
        int? NodeId,
        string? Protocol,
        string? TcpHeaderFormat,
        string? TcpFooter,
        bool TcpHiLo,
        string? ConnType,
        string? IpAddress,
        string? Port,
        int? MaxConn,
        int? RetryDelay,
        bool AlwaysConnected,
        bool OneSocketOnly,
        string? QueueInbox,
        string? QueueOutbox,
        string? WsUrl,
        string? WsMethod,
        string? WsContent,
        DateTime? LastConnected,
        DateTime? LastDisconnected);
}
