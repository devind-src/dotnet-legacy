namespace SyncNetApi.Dtos.CardProducts
{
    /// <summary>ProfileName/LastSeqNr are shown in the legacy grid but never written by any
    /// form (read-only/derived, presumably an external process) — never set by
    /// Create/UpdateAsync.</summary>
    public record CardProductDto(
        int Id,
        string? Issuer,
        string? Product,
        string? PanPrefix,
        short? PanLength,
        string? CardType,
        string? ProfileName,
        int? LastSeqNr);
}
