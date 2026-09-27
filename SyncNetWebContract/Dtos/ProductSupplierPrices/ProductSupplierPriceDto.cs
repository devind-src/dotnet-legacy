namespace SyncNetApi.Dtos.ProductSupplierPrices
{
    public record ProductSupplierPriceDto(int Id, string? SupplierId, string? BillerCode, string? ProductName, int? Denom, int? HargaBeli, int? HargaJual, int? Margin, bool Active);
}
