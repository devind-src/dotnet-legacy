namespace SyncNetApi.Dtos.Merchants
{
    /// <summary>Field set mirrors what the legacy PosBase &gt; Merchant form actually exposes
    /// (dest_acc, contact, online_settlement, schedule_*, group_name exist as DB columns but
    /// are not surfaced anywhere in the legacy UI, so they are left off here too).</summary>
    public record MerchantDto(
        string MerchantId,
        string? ParticipantId,
        string? MccCode,
        string? Name,
        string? Address,
        string? City,
        string? Zipcode,
        string? Phone,
        string? Fax,
        string? Email,
        string? Person,
        string? BankName,
        string? Branch,
        string? DestBank,
        string? AccNumber,
        string? OwnerName,
        bool Active,
        string? VaName,
        string? DateJoin,
        string? Brand,
        string? VirtualAccount,
        string? MidExt,
        string? Nmid,
        // Key Management (manual entry — see SwMerchantKey entity note)
        string? KeyLength,
        string? PinblockFormat,
        string? MasterKey,
        string? MasterKcv,
        string? KeyUnderLmk,
        string? KeyUnderZmk,
        string? KeyCheckValue);
}
