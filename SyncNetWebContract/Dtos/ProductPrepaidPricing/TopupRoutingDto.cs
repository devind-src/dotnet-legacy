namespace SyncNetApi.Dtos.ProductPrepaidPricing
{
    /// <summary>Master Product &gt; Prices &gt; Supplier Prices: produk topup beserta Routing Mode
    /// (sw_routes_margin). HasRoute = sudah punya baris sw_routes_margin (tanpa baris, produk
    /// dirutekan statis lewat Routing &gt; Product seperti sebelumnya).</summary>
    public record TopupRoutingDto(
        string ProductId,
        string? ProductName,
        bool HasRoute,
        string RoutingMode,
        int? StaticNodeId,
        string? StaticNodeName,
        int ActiveSupplierCount,
        bool HasSourceRule);
}
