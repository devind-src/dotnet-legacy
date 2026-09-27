using System.Collections.Generic;

namespace SyncNetApi.Dtos.RouteProducts
{
    /// <summary>Backs Routing &gt; Product (`/route-product`, table `sw_routes_by_inst`) — a
    /// per-product-code routing rule, distinct from the "Product" menu/Product Master.
    /// AlternateNodeIds = node biller cadangan aktif, urut priority (rank 2, 3, dst; rank 1 =
    /// NodeId). HasSourceRule = produk punya rule di Routing &gt; Source, failover tidak boleh
    /// dinyalakan.</summary>
    public record RouteProductDto(string InstId, int NodeId, string? Notes, string RoutingMode,
        IReadOnlyList<int> AlternateNodeIds, bool HasSourceRule);
}
