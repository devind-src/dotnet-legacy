namespace SyncNetApi.Dtos.RouteProductAlts
{
    /// <summary>Satu biller alternate untuk produk bill payment. Priority minimal 2 (rank 1 =
    /// primary di Routing &gt; Product).</summary>
    public record RouteProductAltDto(int Id, string InstId, int NodeId, int Priority, string? Notes, bool Active);
}
