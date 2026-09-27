namespace SyncNetApi.Dtos.Apps
{
    /// <summary>Path/LastUpdate are read-only display fields — not exposed in the legacy form,
    /// never written by this API's Create/Update.</summary>
    public record AppDto(string AppName, string? Host, string? AppType, string? CommandPort, bool Active, string? Path, DateTime? LastUpdate);
}
