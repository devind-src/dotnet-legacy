namespace SyncNetApi.Dtos.Queries
{
    public record AuditDetailDto(long Id, string? Type, string? TableName, string? UserName, DateTime? UpdateDate, string? OldValue, string? NewValue);
}
