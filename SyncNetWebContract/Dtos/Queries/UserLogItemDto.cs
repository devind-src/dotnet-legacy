namespace SyncNetApi.Dtos.Queries
{
    public record UserLogItemDto(long LogId, string? UserName, string? State, DateTime? DateTime, string? HostAddress, string? HostName, string? HostAgent);
}
