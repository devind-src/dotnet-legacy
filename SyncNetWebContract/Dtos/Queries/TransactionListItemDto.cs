namespace SyncNetApi.Dtos.Queries
{
    public record TransactionListItemDto(
        long TranNr,
        DateTime? TimeReq,
        string? SourceNode,
        string? DestNode,
        string? MaskedPan,
        string? TranType,
        string? TranTypeExt,
        decimal? Amount,
        string? ReceivingInstId,
        string? ToAccNumber,
        string? MerchantId,
        string? TerminalId,
        string? RespCode);
}
