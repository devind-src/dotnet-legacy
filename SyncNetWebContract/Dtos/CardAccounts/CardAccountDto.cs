namespace SyncNetApi.Dtos.CardAccounts
{
    /// <summary>GroupName and AccTypeName are JOIN-time values (sw_group / sw_account_types),
    /// not physical columns on sw_accounts — mirrors the legacy list's "Group Name"/"Acc Name"
    /// columns.</summary>
    public record CardAccountDto(
        int Id,
        int? GroupId,
        string? GroupName,
        string? AccType,
        string? AccTypeName,
        bool PanVerification,
        bool PinVerification);
}
