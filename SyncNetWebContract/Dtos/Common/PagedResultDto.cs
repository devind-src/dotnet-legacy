namespace SyncNetApi.Dtos.Common
{
    /// <summary>Server-side pagination result — used by Queries &gt; Transaction/Audit Trail/User
    /// Log (§7.27), the first module in this project where result sets are large enough that
    /// the usual "load everything, page client-side with MudTablePager" pattern doesn't scale
    /// (sw_audit/dashboard_user_log already have hundreds of rows from dev testing alone).</summary>
    public record PagedResultDto<T>(IReadOnlyList<T> Items, long TotalRecords, int PageNumber, int PageSize)
    {
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalRecords / PageSize);
    }
}
