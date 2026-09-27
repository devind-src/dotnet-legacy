namespace SyncNetApi.Dtos.ProductMappings
{
    public record ProductMappingDto(long Id, string? AppName, string? SourceBillerCode, int? Denom, string? DestBillerCode, bool IsDeposit, string? Notes);
}
