namespace SyncNetApi.Dtos.Countries
{
    public record CountryDto(string Name, string CodeAlpha2, string CodeAlpha3, int CodeNumeric, bool Active);
}
