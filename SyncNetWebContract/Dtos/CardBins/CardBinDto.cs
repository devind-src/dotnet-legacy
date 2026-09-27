namespace SyncNetApi.Dtos.CardBins
{
    /// <summary>GroupName is a JOIN-time value from sw_group, not a physical column on
    /// sw_bins — mirrors the legacy list's "Group Name" column.</summary>
    public record CardBinDto(string BinNr, int GroupId, string? GroupName, string? BinDesc);
}
