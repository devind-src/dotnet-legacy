namespace SyncNetApi.Dtos.TerminalLimits
{
    /// <summary>LimitName is immutable after create (matches legacy UI, read-only on edit) —
    /// GroupName and the min/max amounts can change.</summary>
    public class UpdateTerminalLimitRequest
    {
        public string? GroupName { get; set; }

        public long MinWithdrawal { get; set; }
        public long MaxWithdrawal { get; set; }
        public long MinTransfer { get; set; }
        public long MaxTransfer { get; set; }
        public long MinPurchase { get; set; }
        public long MaxPurchase { get; set; }
        public long MinPayment { get; set; }
        public long MaxPayment { get; set; }
    }
}
