namespace SyncNetApi.Dtos.JobCleaners
{
    public record JobCleanerDto(string Entity, int? Period, string? Description, bool Active);
}
