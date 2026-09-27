namespace SyncNetApi.Dtos.Queries
{
    public class UserLogSearchRequest
    {
        public DateTime? DateStart { get; set; }
        public DateTime? DateEnd { get; set; }
        public string? UserName { get; set; }

        /// <summary>"1" = Login, "0" = Logout, null/empty = both.</summary>
        public string? State { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }
}
