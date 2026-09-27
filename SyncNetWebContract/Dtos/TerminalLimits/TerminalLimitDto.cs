namespace SyncNetApi.Dtos.TerminalLimits
{
    public record TerminalLimitDto(
        long Id,
        string LimitName,
        string? GroupName,
        long MinWithdrawal,
        long MaxWithdrawal,
        long MinTransfer,
        long MaxTransfer,
        long MinPurchase,
        long MaxPurchase,
        long MinPayment,
        long MaxPayment);
}
