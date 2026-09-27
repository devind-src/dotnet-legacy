using System;

namespace SyncNetApi.Dtos.RouteSchedules
{
    /// <summary>Aturan Jadwal Routing. State dan WindowText dihitung dari jam server
    /// (RouteScheduleCodes.State*). ActiveNow = jendela sedang berlaku; ActiveUntil = akhir jendela
    /// yang sedang berlaku. CanEdit/CanEditEndOnly/CanCancel/CanDelete mengikuti aturan riwayat
    /// (FASE-3 §3.6): Selesai/Dibatalkan read-only, Berlangsung hanya jam selesai, hapus hanya
    /// Akan datang.</summary>
    public record RouteScheduleDto(
        int Id,
        string RuleName,
        string RuleType,
        string? RoutingType,
        string? InstId,
        string? ProductName,
        int? NodeId,
        string? NodeName,
        short? Priority,
        string Recurrence,
        DateTime? StartDt,
        DateTime? EndDt,
        TimeSpan? TimeStart,
        TimeSpan? TimeEnd,
        string? DaysOfWeek,
        string? DaysOfMonth,
        DateOnly? ValidFrom,
        DateOnly? ValidUntil,
        string? Notes,
        bool IsActive,
        string? CancelReason,
        string? CancelledBy,
        DateTime? CancelledDt,
        string? CreatedBy,
        DateTime? CreatedDt,
        string? UpdatedBy,
        DateTime? UpdatedDt,
        string State,
        string WindowText,
        bool ActiveNow,
        DateTime? ActiveUntil,
        bool CanEdit,
        bool CanEditEndOnly,
        bool CanCancel,
        bool CanDelete);
}
