namespace SyncNetApi.Dtos.Tools
{
    /// <summary>One decoded BER-TLV entry — Tag/ValueHex are hex strings, TagName is looked up
    /// from the legacy NbTlvEmv tag dictionary ("Unknown Tag" if not recognized).</summary>
    public record IccTlvEntryDto(string Tag, int Length, string ValueHex, string TagName);
}
