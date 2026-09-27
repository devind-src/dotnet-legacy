namespace SyncNetApi.Dtos.CardIssuers
{
    /// <summary>Flat DTO combining cms_issuers + its 1:1 cms_issuer_contacts + cms_issuer_keys
    /// rows, matching the legacy single-dialog 5-tab form (General/Limit/Contact/Business
    /// Address/Key Management). Only the address_1/city_1/postal_1 triplet of
    /// cms_issuer_contacts is exposed (see entity note) — address_2/3 are dead from this app's
    /// perspective.</summary>
    public record CardIssuerDto(
        string Issuer,
        string? InstId,
        string? Currency,
        bool AuthService,
        bool VelocityPerTran,
        bool VelocityDaily,
        bool VelocityWeekly,
        bool VelocityMonthly,
        int? MaxPinTries,
        string? ContactName,
        string? Phone,
        string? Fax,
        string? Mobile,
        string? Email,
        string? BusinessAddress,
        string? City,
        string? PostalCode,
        string? KeyLength,
        string? PinblockFormat,
        string? MasterKey,
        string? MasterKcv,
        string? KeyUnderLmk,
        string? KeyUnderZmk,
        string? KeyCheckValue);
}
