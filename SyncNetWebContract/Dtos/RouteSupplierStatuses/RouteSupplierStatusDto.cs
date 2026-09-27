using System;

namespace SyncNetApi.Dtos.RouteSupplierStatuses
{
    /// <summary>ConsecutiveSuspectCount = counter Timeout. BlockReason: LINK_DOWN, TIMEOUT,
    /// RC_FAILED, PENDING, LATENCY (null saat ACTIVE).</summary>
    public record RouteSupplierStatusDto(
        string SupplierId,
        string Status,
        string? LastRcCode,
        int ConsecutiveSuspectCount,
        int RetryCount,
        DateTime? LastTranDt,
        DateTime? BlockedSince,
        DateTime? BlockedUntil,
        string? UpdatedBy,
        DateTime? UpdatedDt,
        int ConsecutiveFailedCount = 0,
        int ConsecutivePendingCount = 0,
        int ConsecutiveLatencyCount = 0,
        string? BlockReason = null,
        int? LastLatencyMs = null);
}
