namespace SyncNetApi.Dtos.Monitoring
{
    public record InterfaceMonitorDto(
        int NodeId,
        string NodeName,
        string? AppName,
        string? PortIn,
        string? PortOut,
        string? InstId,
        DateTime? LastEcho,
        string? Status);
}
