namespace SyncNetApi.Dtos.JobFees
{
    public record JobFeeDto(long Id, string? JobDesc, bool IsDone, DateTime? ScheduledAt, DateTime? CreatedAt, int DetailCount);
}
