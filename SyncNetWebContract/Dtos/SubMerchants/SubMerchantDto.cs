namespace SyncNetApi.Dtos.SubMerchants
{
    public record SubMerchantDto(
        string SubMerchantId,
        string? SubMerchantIdExt,
        string? MerchantId,
        string? Name,
        string? Address,
        string? City,
        string? Zipcode,
        string? Phone,
        string? Fax,
        string? Email,
        bool Active,
        string? DateJoin);
}
