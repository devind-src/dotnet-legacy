namespace SyncNetApi.Dtos.ProductMerchantPricing
{
    /// <summary>MerchantName is resolved via join for display — mirrors legacy
    /// VwMarginMerchant.</summary>
    public record ProductMerchantPricingDto(int Id, string? MerchantId, string? MerchantName, string? BillerCode, string? ProductName, int? Denom, int? HargaJual);
}
