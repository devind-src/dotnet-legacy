namespace SyncNetApi.Dtos.VaAccounts
{
    public record VaAccountDto(
        string AccNr,
        string? GroupName,
        string? VaName,
        string? VaNotes,
        bool Active,
        decimal LowBalance,
        decimal MinBalance,
        decimal Balance);
}
