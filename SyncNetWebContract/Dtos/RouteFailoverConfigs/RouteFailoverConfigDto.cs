namespace SyncNetApi.Dtos.RouteFailoverConfigs
{
    /// <summary>RcSuspect / MaxConsecutiveSuspect / SuspectCooldownMinutes = kategori Timeout.
    /// RcFailed, RcPending, LatencyThresholdSeconds null = kategori nonaktif.</summary>
    public record RouteFailoverConfigDto(
        int Id,
        string RoutingType,
        string? InstId,
        string RcLinkDown,
        string RcSuspect,
        int MaxConsecutiveSuspect,
        int? LinkDownCooldownMinutes,
        int? SuspectCooldownMinutes,
        bool IsActive,
        string? RcFailed = null,
        int MaxConsecutiveFailed = 3,
        int? FailedCooldownMinutes = null,
        string? RcPending = null,
        int MaxConsecutivePending = 5,
        int? PendingCooldownMinutes = null,
        decimal? LatencyThresholdSeconds = null,
        int MaxConsecutiveLatency = 3,
        int? LatencyCooldownMinutes = null);
}
