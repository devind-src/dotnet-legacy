namespace SyncNetApi.Dtos.JobSchedules
{
    /// <summary>LastRunning/Status/ReadOnly are read-only display fields — never written by
    /// this API's Create/Update (see entity note).</summary>
    public record JobScheduleDto(
        int JobId,
        string? JobName,
        string? FreqFlag,
        string? FreqOnce,
        int? FreqNumber,
        string? FreqStart,
        string? FreqEnd,
        bool Active,
        string? RunAt,
        string? AppPath,
        DateTime? LastRunning,
        string? Status,
        bool ReadOnly);
}
