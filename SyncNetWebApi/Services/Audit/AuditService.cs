using System.Text.Json;
using SyncNetApi.Data;

namespace SyncNetApi.Services.Audit
{
    /// <summary>
    /// Thin wrapper around SyncNetDbContext's audit helpers so callers pass plain objects
    /// instead of pre-serialized JSON strings. Does not call SaveChangesAsync itself —
    /// audit rows are added to the same DbContext change tracker as the entity being
    /// modified, so both are committed atomically by the caller's single SaveChangesAsync.
    /// </summary>
    public class AuditService : IAuditService
    {
        private readonly SyncNetDbContext _context;
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

        public AuditService(SyncNetDbContext context)
        {
            _context = context;
        }

        public void LogInsert(object newData, string tableName, string entryBy)
            => _context.AddAuditInsert(Serialize(newData), tableName, entryBy);

        public void LogUpdate(object oldData, object newData, string tableName, string entryBy)
            => _context.AddAuditUpdate(Serialize(oldData), Serialize(newData), tableName, entryBy);

        public void LogDelete(object oldData, string tableName, string entryBy)
            => _context.AddAuditDelete(Serialize(oldData), tableName, entryBy);

        // sw_audit.oldvalue/newvalue are `json` columns in Postgres — "null" (the JSON
        // literal) is the valid representation of "no data", not an empty string.
        private static string Serialize(object? data) =>
            data == null ? "null" : JsonSerializer.Serialize(data, JsonOptions);
    }
}
