namespace SyncNetApi.Dtos.Lookups
{
    /// <summary>Generic key/value pair for populating dropdowns backed by tables outside
    /// PosBase's own scope (e.g. Card &gt; Group, Configuration &gt; Base &gt; City) — read-only,
    /// no CRUD is exposed against these tables here.</summary>
    public record LookupItemDto(string Key, string Value);
}
