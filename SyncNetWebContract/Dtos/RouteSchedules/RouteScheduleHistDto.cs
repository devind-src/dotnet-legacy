using System;

namespace SyncNetApi.Dtos.RouteSchedules
{
    /// <summary>Riwayat aturan: nilai SEBELUM perubahan (Action UPDATE / CANCEL / DELETE).</summary>
    public record RouteScheduleHistDto(
        long HistId,
        int ScheduleId,
        string Action,
        string? RuleName,
        string? RuleType,
        string? RoutingType,
        string? InstId,
        int? NodeId,
        string? NodeName,
        short? Priority,
        string? Recurrence,
        string WindowText,
        string? Notes,
        string? Status,
        string? CancelReason,
        string ChangedBy,
        DateTime ChangedDt);
}
