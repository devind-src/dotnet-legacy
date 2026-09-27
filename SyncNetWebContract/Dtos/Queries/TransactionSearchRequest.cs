namespace SyncNetApi.Dtos.Queries
{
    /// <summary>Bound from query string ([FromQuery]) — mirrors legacy Queries &gt; Transaction's
    /// filter panel (Source/Dest Node, Merchant/Terminal ID, Account No, Resp Code, Trace/Ref
    /// Number) plus a date range that defaults to "today" on the frontend, same as legacy.</summary>
    public class TransactionSearchRequest
    {
        public DateTime? DateStart { get; set; }
        public DateTime? DateEnd { get; set; }
        public string? SourceNode { get; set; }
        public string? DestNode { get; set; }
        public string? MerchantId { get; set; }
        public string? TerminalId { get; set; }
        public string? AccountNo { get; set; }
        public string? RespCode { get; set; }
        public string? TraceNumber { get; set; }
        public string? RefNumber { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }
}
