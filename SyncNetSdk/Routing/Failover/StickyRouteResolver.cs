using SyncNet.DbRepository;
using SyncNet.Models;
using System.Threading.Tasks;

namespace SyncNet.Routing.Failover
{
    // satu siklus transaksi (inquiry, payment/topup, advice/reversal) harus ke biller yang
    // sama. biller dipilih sekali di transaksi pertama siklus lalu dicatat di
    // sw_routes_tran_map; transaksi berikutnya hanya membaca catatan itu.
    public class StickyRouteResolver
    {
        private readonly DbMgr _dbMgr;

        public StickyRouteResolver()
        {
            _dbMgr = new DbMgr();
        }

        // format sama dengan DataHelper.GetSwitchKey di core
        public static string BuildSwitchKey(string tranType, string datetimeTran, string traceNumber, string terminalId)
        {
            return (tranType + datetimeTran + traceNumber + terminalId).ToUpper();
        }

        // format sama dengan DataHelper.GetSwitchKeyOrig di core
        public static string BuildOriginalSwitchKey(string originalData, string terminalId)
        {
            return (originalData + terminalId).ToUpper();
        }

        // kunci komposit siklus; refnum sama untuk inquiry, payment, dan advice
        public Task<TranMapModel> FindByRefAsync(string merchantId, string terminalId, string refnum)
        {
            return _dbMgr.GetTranMapByRef(merchantId, terminalId, refnum);
        }

        public Task<long> SaveAsync(TranMapModel entry)
        {
            return _dbMgr.InsertTranMap(entry);
        }

        public Task SetSwitchKeyAsync(long id, string switchKey)
        {
            return _dbMgr.UpdateTranMapSwitchKey(id, switchKey);
        }

        // advice/reversal: cari siklus lewat switch_key payment asal, lalu lewat refnum,
        // terakhir lewat node tujuan yang dicatat core pada transaksi asal.
        public async Task<string> FindNodeForOriginalAsync(string merchantId, string terminalId,
            string refnum, string originalSwitchKey)
        {
            var entry = await _dbMgr.GetTranMapBySwitchKey(originalSwitchKey);
            if (entry != null) return entry.NodeName;

            if (string.IsNullOrEmpty(refnum) == false)
            {
                entry = await _dbMgr.GetTranMapByRef(merchantId, terminalId, refnum);
                if (entry != null) return entry.NodeName;
            }

            return await _dbMgr.GetDestNodeBySwitchKey(originalSwitchKey);
        }
    }
}
