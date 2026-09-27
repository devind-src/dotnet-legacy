namespace SyncNetApi.Dtos.Tools
{
    /// <summary>Shared single-value result shape for DES Calculator, Pinblock Calculator and
    /// Encrypt Credential — all three just produce one result string.</summary>
    public record ToolResultDto(string Result);
}
