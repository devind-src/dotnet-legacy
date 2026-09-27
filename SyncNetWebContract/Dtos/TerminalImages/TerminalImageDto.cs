namespace SyncNetApi.Dtos.TerminalImages
{
    public record TerminalImageDto(
        string Name,
        string? UrlBase,
        string? LogoUrl,
        string? Banner1Url,
        string? Banner2Url,
        string? Banner3Url,
        string? Banner4Url,
        string? Banner5Url);
}
