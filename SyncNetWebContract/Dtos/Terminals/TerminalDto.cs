namespace SyncNetApi.Dtos.Terminals
{
    /// <summary>Field set mirrors every tab of the legacy PosBase &gt; Terminal form exactly:
    /// General, Group, Device, Security, Bank, Key Management. Key Management fields are
    /// plain manual entry — legacy populates them via a real HSM (NbHSM) key ceremony that
    /// has no equivalent here (see SwTerminalKey entity note).</summary>
    public record TerminalDto(
        // General
        string TermId,
        string? MerchantId,
        string? MerchantName,
        string? SubMerchantId,
        string? StoreId,
        string? LoketName,
        string? Location,
        string? City,
        string? Phone,
        bool Active,
        // Group
        string? GroupName,
        string? SubgroupName,
        string? Criteria,
        string? Nmid,
        string? Mpan,
        string? ChatId,
        // Device
        string? Brand,
        string? Type,
        string? SerialNumber,
        bool EnableInit,
        string? InitId,
        string? InstDate,
        // Security
        bool Security,
        string? IpAddress,
        string? MacAddress,
        // Bank
        string? BankName,
        string? BankBranch,
        string? BankCode,
        string? BankAccNumber,
        string? BankAccName,
        string? VirtualAccount,
        string? VaName,
        // Key Management
        string? KeyLength,
        string? PinblockFormat,
        string? MasterKey,
        string? MasterKcv,
        string? KeyUnderLmk,
        string? KeyUnderZmk,
        string? KeyCheckValue);
}
