namespace SyncNetApi.Dtos.Monitoring
{
    public record ServiceMonitorDto(string AppName, string? AppType, string? Status, DateTime? LastUpdate);
}
