namespace SyncNetApi.Dtos.ProductPostpaidBillers
{
    /// <summary>Satu biller untuk produk bill payment &amp; purchase (detail Product &gt; Fees &gt;
    /// Product Fees). Priority 1 = primary (sw_routes_by_inst), lainnya alternate
    /// (sw_routes_by_inst_alt). BillerProductCode dari Product &gt; Mapping menurut app_name node.
    /// HealthStatus dari sw_routes_supplier_status (ACTIVE bila belum pernah tercatat).</summary>
    public record ProductPostpaidBillerDto(
        string ProductId,
        int NodeId,
        string NodeName,
        string? AppName,
        string? BillerProductCode,
        int Priority,
        bool IsPrimary,
        int? FeeSharing,
        int? LbWeight,
        string? Notes,
        bool Active,
        string HealthStatus);

    /// <summary>Daftar biller satu produk. HasSourceRule = produk punya rule di Routing &gt;
    /// Source, sehingga mode dynamic tidak boleh dipakai.</summary>
    public record ProductPostpaidBillersDto(string ProductId, string? ProductName, bool HasSourceRule,
        IReadOnlyList<ProductPostpaidBillerDto> Billers);
}
