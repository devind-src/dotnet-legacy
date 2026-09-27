namespace SyncNetApi.Services.Audit
{
    public interface IAuditService
    {
        void LogInsert(object newData, string tableName, string entryBy);
        void LogUpdate(object oldData, object newData, string tableName, string entryBy);
        void LogDelete(object oldData, string tableName, string entryBy);
    }
}
