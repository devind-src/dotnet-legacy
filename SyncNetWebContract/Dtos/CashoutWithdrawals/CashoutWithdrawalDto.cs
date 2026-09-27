namespace SyncNetApi.Dtos.CashoutWithdrawals
{
    public record CashoutWithdrawalDto(long Id, string? TerminalId, string? MerchantId, string? BankName, string? BankCode, string? AccNumber, string? AccName);
}
