using System;

namespace SyncNetApi.Dtos.RouteFailoverLogs
{
    /// <summary>Reason: LINK_DOWN, TIMEOUT, RC_FAILED, PENDING, LATENCY (baris lama: SUSPECT_TIMEOUT),
    /// SCHEDULE_CLOSED (Fase 3: transaksi ditolak X15 karena Jadwal Routing, ScheduleId = aturannya).</summary>
    public record RouteFailoverLogDto(
        int Id,
        string RoutingType,
        string? InstId,
        int? Denom,
        string? TraceNumber,
        string? FromSupplierId,
        string? ToSupplierId,
        string? RcCode,
        string? Reason,
        DateTime CreatedDt,
        int? LatencyMs = null,
        int? ScheduleId = null);
}
