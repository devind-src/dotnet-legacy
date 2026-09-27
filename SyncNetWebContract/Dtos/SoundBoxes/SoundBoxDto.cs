namespace SyncNetApi.Dtos.SoundBoxes
{
    public record SoundBoxDto(
        string Nmid,
        string StoreName,
        string? Address,
        string? City,
        string SerialNumber,
        string Provider,
        string? GroupName,
        string? SubgroupName);
}
