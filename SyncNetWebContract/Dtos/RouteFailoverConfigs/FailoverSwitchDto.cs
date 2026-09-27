namespace SyncNetApi.Dtos.RouteFailoverConfigs
{
    /// <summary>Tombol darurat failover per kategori (RoutingType MARGIN = topup, PRODUCT = bill
    /// payment). Enabled mengikuti `is_active` baris config global. HasGlobalRow = false berarti
    /// belum ada baris global, dan failover berjalan dengan default bawaan.</summary>
    public record FailoverSwitchDto(string RoutingType, bool Enabled, bool HasGlobalRow);
}
