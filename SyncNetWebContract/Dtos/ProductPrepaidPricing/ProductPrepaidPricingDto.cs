namespace SyncNetApi.Dtos.ProductPrepaidPricing
{
    public record ProductPrepaidPricingDto(int Id, string? SupplierId, string? BillerCode, string? ProductName, int? Denom, int? HargaBeli, int? HargaJual, int? Margin, bool Active,
        int? Priority = null, int? LbWeight = null);
}
