namespace SyncNetApi.Dtos.Currencies
{
    public record CurrencyDto(string CurrencyCode, string? AlphaCode, string Name, int NrDecimals, double Rate, bool Active);
}
