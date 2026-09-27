using SyncNet.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SyncNet.Routing.Failover
{
    // penyimpanan status kesehatan supplier. implementasi produksi = DbMgr; unit test memakai
    // implementasi di memori.
    public interface ISupplierStatusStore
    {
        Task<List<FailoverModel.SupplierStatus>> GetSupplierStatus();
        Task<List<FailoverModel.Config>> GetFailoverConfig();
        Task UpsertSupplierStatus(FailoverModel.SupplierStatus status);
        Task InsertFailoverLog(string routingType, string instId, long? denom, string traceNumber,
            string fromSupplierId, string toSupplierId, string rcCode, string reason, int? latencyMs = null, int? scheduleId = null);
    }
}
