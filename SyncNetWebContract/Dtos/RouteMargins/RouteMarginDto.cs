namespace SyncNetApi.Dtos.RouteMargins
{
    /// <summary>ActiveSupplierCount = jumlah supplier aktif berbeda untuk produk ini di
    /// sw_margin_supplier. Failover baru boleh dinyalakan bila minimal 2. HasSourceRule = produk
    /// punya rule di Routing > Source, sehingga failover tidak boleh dinyalakan.</summary>
    public record RouteMarginDto(string InstId, string? Notes, string RoutingMode, int ActiveSupplierCount, bool HasSourceRule);
}
