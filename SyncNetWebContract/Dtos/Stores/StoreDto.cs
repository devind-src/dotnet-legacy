namespace SyncNetApi.Dtos.Stores
{
    public record StoreDto(
        string StoreId,
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
