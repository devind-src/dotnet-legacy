namespace SyncNetApi.Dtos.Monitoring
{
    public record TerminalMonitorDto(
        string TermId,
        string? MerchantId,
        string? Location,
        string? Brand,
        string? Type,
        string? SerialNumber,
        string? Status);
}
