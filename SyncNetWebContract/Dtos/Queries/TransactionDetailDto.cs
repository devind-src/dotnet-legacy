namespace SyncNetApi.Dtos.Queries
{
    /// <summary>Field-for-field mirror of legacy Queries &gt; Transaction &gt; Detail's "General" tab,
    /// grouped the same way (General/Amount/DateTime/Terminal/DataRequestResponse) so the
    /// frontend can render one MudExpansionPanel per group without re-deriving the grouping.
    /// <paramref name="Reversal"/> is populated the same way legacy TransGetReversal does — one
    /// level only (a reversal's own Reversal is always null, matches legacy's non-recursive
    /// lookup). MaskedPan is masked server-side (see StringExtensions.MaskPan); track2data is
    /// intentionally not exposed at all (PCI-sensitive, legacy detail view doesn't show it
    /// either).</summary>
    public record TransactionDetailDto(
        // General
        long TranNr,
        string? SwitchKey,
        short? State,
        string? SourceNode,
        string? DestNode,
        string? MaskedPan,
        string? PosEntryMode,
        string? MsgType,
        string? TranType,
        string? TranTypeExt,
        string? FromAccType,
        string? ToAccType,
        string? AcqInstId,
        string? FwdInstId,
        string? ReceivingInstId,
        string? FromAccNumber,
        string? ToAccNumber,
        string? VaAccNumber,
        string? TraceNumber,
        string? ReffNumber,
        string? SourceTran,
        string? AuthTran,
        string? TranReversed,
        string? IpEndpoint,

        // Amount
        decimal? AmountTranReq,
        decimal? AmountTranRsp,
        decimal? AmountVa,
        string? AdditionalAmount,
        decimal? LastBalance,
        decimal? FeeTotal,
        decimal? FeeSwt,
        decimal? FeeAcq,
        decimal? FeeMer,
        decimal? FeeSub,
        decimal? FeeBil,
        decimal? FeeIss,
        string? Currency,

        // Date Time
        string? TranDatetime,
        string? DateSettleReq,
        string? DateSettleRsp,
        DateTime? TimeReq,
        DateTime? TimeRsp,

        // Terminal
        string? MerchantId,
        string? MerchantType,
        string? TerminalId,

        // Data Request/Response
        string? RespCodeRsp,
        string? RespCodeRev,
        string? RespCodeAdv,
        string? IccData,
        string? OrigData,
        string? NodeDataReq,
        string? NodeDataRsp,

        TransactionDetailDto? Reversal);
}
