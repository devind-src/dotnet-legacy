namespace SyncNetApi.Dtos.Queries
{
    public class AuditSearchRequest
    {
        public DateTime? DateStart { get; set; }
        public DateTime? DateEnd { get; set; }
        public string? TableName { get; set; }
        public string? UserName { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }
}
