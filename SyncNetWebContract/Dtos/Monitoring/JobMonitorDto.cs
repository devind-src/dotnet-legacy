namespace SyncNetApi.Dtos.Monitoring
{
    public record JobMonitorDto(string JobName, DateTime? LastRunning, string? Status);
}
