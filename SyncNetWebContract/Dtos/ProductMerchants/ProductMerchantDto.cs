namespace SyncNetApi.Dtos.ProductMerchants
{
    /// <summary>MerchantName/ProductName/Category are resolved via join for display — mirrors
    /// legacy VwMerchantProduct (Category is the joined Product Master's category, not a
    /// column of sw_merchant_product itself).</summary>
    public record ProductMerchantDto(int Id, string? MerchantId, string? MerchantName, string? ProductId, string? ProductName, string? Category, string? Notes, bool Active);
}
