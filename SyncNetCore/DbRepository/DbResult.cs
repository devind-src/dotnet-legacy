namespace SyncNet.DbRepository
{
    public class DbResult
    {
        public bool IsSuccess { get; set; }
        public int AffectedRows { get; set; }
        public string ErrorMessage { get; set; }
    }
}
