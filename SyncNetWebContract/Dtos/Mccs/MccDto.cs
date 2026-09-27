namespace SyncNetApi.Dtos.Mccs
{
    public record MccDto(string MccCode, string? MccDesc, string? FloorLimit, string? Currency, bool Active);
}
