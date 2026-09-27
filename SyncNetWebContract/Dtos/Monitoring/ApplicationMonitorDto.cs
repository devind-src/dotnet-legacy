namespace SyncNetApi.Dtos.Monitoring
{
    public record ApplicationMonitorDto(string AppName, string? AppType, string? Host, string? CommandPort, string? Status);
}
