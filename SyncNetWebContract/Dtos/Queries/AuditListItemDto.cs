namespace SyncNetApi.Dtos.Queries
{
    public record AuditListItemDto(long Id, string? Type, string? TableName, string? UserName, DateTime? UpdateDate);
}
