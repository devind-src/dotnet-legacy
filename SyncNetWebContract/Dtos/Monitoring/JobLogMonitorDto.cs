namespace SyncNetApi.Dtos.Monitoring
{
    public record JobLogMonitorDto(long JobNr, string? JobName, DateTime? DatetimeBegin, DateTime? DatetimeEnd, int ResultValue, string? Status);
}
