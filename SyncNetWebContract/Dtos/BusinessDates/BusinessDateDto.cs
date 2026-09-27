namespace SyncNetApi.Dtos.BusinessDates
{
    /// <summary>CurrentBsnDate/PreviousBsnDate are read-only display fields — managed by an
    /// external batch/EOD process, never written by this API's Create/Update.</summary>
    public record BusinessDateDto(
        string BusinessCalendar,
        string? TimeCutover,
        string? CurrentBsnDate,
        string? PreviousBsnDate,
        bool EnableClosing,
        string? TimeStart,
        string? TimeEnd,
        bool Sun,
        bool Mon,
        bool Tue,
        bool Wed,
        bool Thu,
        bool Fri,
        bool Sat,
        bool Active);
}
