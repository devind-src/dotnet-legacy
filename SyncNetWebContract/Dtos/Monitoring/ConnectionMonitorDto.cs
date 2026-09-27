namespace SyncNetApi.Dtos.Monitoring
{
    public record ConnectionMonitorDto(
        string ConnName,
        string? ConnType,
        string? Protocol,
        string? IpAddress,
        string? Port,
        string? WsUrl,
        string? WsMethod,
        string? WsContent,
        short? Remote);
}
