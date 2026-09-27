namespace SyncNetApi.Dtos.Nodes
{
    /// <summary>Flat DTO combining sw_nodes + its 1:1 sw_crypto_keys row, matching the legacy
    /// single-page 3-tab form (General/Option/Key Management). PortIn/PortOut/LastConnected/
    /// LastDisconnected/LastEcho are read-only display fields (see entity notes).</summary>
    public record NodeDto(
        int NodeId,
        string? NodeName,
        string? AppName,
        string? BusinessCalendar,
        string? InstId,
        string? Parameter,
        string? PortIn,
        string? PortOut,
        int? TeamId,
        string? TeamName,
        string? Category,
        bool AutoSignon,
        string? AutoReversal,
        bool AutoReplyReversal,
        short? RequestTimeout,
        short? AdviceTimeout,
        short? KeychangeTimer,
        short? EchoTimer,
        int? SafLimit,
        bool PinTranslate,
        bool SensitiveData,
        bool SaveRepeatReversal,
        DateTime? LastConnected,
        DateTime? LastDisconnected,
        DateTime? LastEcho,
        string? KeyLength,
        string? PinblockFormat,
        string? MasterKey,
        string? MasterKcv,
        string? KeyUnderLmk,
        string? KeyUnderZmk,
        string? KeyCheckValue);
}
