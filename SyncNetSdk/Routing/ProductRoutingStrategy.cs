using SyncNet.Constants;
using SyncNet.DbRepository;
using SyncNet.Routing.Failover;
using SyncNet.Routing.Schedule;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SyncNet.Routing
{
    // routing bill payment & purchase. kandidat = primary (sw_routes_by_inst) lalu alternate
    // (sw_routes_by_inst_alt) menurut priority. routing mode datang dari baris Product Fees
    // CA/default (BillPaymentFeeCalculator.GetRoutingMode):
    //   PRIORITY     : primary-backup menurut priority
    //   BEST_PRICE   : sharing fee terbesar (= fee switch terbesar), tie-break priority
    //   LOAD_BALANCE : acak berbobot lb_weight di antara biller yang sehat
    // STATIC ditangani RoutingResolver (tanpa failover).
    // Jadwal Routing (Fase 3): biller yang Tutup / di luar Jam Operasional dikeluarkan sebelum health
    // check, biller dengan Prioritas Jadwal didahulukan; tidak ada biller buka = ditolak.
    public class ProductRoutingStrategy
    {
        private class Candidate
        {
            public int NodeId;
            public string NodeName;
            public int Priority;
            public int? FeeSharing;
            public int LbWeight;
        }

        private class ProductRoute
        {
            public List<Candidate> Candidates = []; // [primary, alternate menurut priority]
        }

        private volatile Dictionary<string, ProductRoute> _routes = [];

        private readonly SupplierStatusRepository _supplierStatus;
        private readonly RoutingScheduleRepository _schedule;
        private readonly DbMgr _dbMgr;

        public ProductRoutingStrategy(SupplierStatusRepository supplierStatus, RoutingScheduleRepository schedule)
        {
            _supplierStatus = supplierStatus;
            _schedule = schedule;
            _dbMgr = new DbMgr();
        }

        public async Task Initialize()
        {
            var routes = new Dictionary<string, ProductRoute>();

            foreach (var obj in await _dbMgr.GetRoutesByProduct())
            {
                routes.TryAdd(obj.ProductId, new ProductRoute
                {
                    Candidates =
                    [
                        new Candidate
                        {
                            NodeId = obj.NodeId,
                            NodeName = obj.NodeName,
                            Priority = 1,
                            FeeSharing = obj.FeeSharing,
                            LbWeight = obj.LbWeight
                        }
                    ]
                });
            }

            foreach (var obj in await _dbMgr.GetRoutesByProductAlt())
            {
                //alternate tanpa primary diabaikan, dan node yang sama dengan kandidat lain dilewati
                if (routes.TryGetValue(obj.ProductId, out var route) == true &&
                    route.Candidates.Any(c => c.NodeName == obj.NodeName) == false)
                {
                    route.Candidates.Add(new Candidate
                    {
                        NodeId = obj.NodeId,
                        NodeName = obj.NodeName,
                        Priority = obj.Priority,
                        FeeSharing = obj.FeeSharing,
                        LbWeight = obj.LbWeight
                    });
                }
            }

            _routes = routes;
        }

        public bool HasRoute(string productId)
        {
            return _routes.ContainsKey(productId);
        }

        public string GetPrimary(string productId)
        {
            return _routes.TryGetValue(productId, out var route) == true && route.Candidates.Count > 0
                ? route.Candidates[0].NodeName
                : null;
        }

        // node untuk mode STATIC; harus salah satu biller produk ini
        public string GetNodeName(string productId, int nodeId)
        {
            return _routes.TryGetValue(productId, out var route) == true
                ? route.Candidates.FirstOrDefault(c => c.NodeId == nodeId)?.NodeName
                : null;
        }

        public int? GetSharingFee(string productId, string nodeName)
        {
            return _routes.TryGetValue(productId, out var route) == true
                ? route.Candidates.FirstOrDefault(c => c.NodeName == nodeName)?.FeeSharing
                : null;
        }

        // routing dynamic aktif bila produk punya route, mode dynamic, dan tombol darurat
        // kategori bill payment tidak dimatikan.
        public bool IsDynamicActive(string productId, string mode)
        {
            return HasRoute(productId) == true &&
                RoutingMode.IsDynamic(mode) == true &&
                _supplierStatus.IsFailoverEnabled(SupplierStatusRepository.ROUTING_PRODUCT) == true;
        }

        // pilihan untuk siklus baru: buang biller yang tutup menurut jadwal, lalu kandidat eligible
        // pertama menurut mode (Prioritas Jadwal di depan). semua yang buka diblokir health = biller
        // buka pertama; tidak ada yang buka = ditolak.
        public Task<RouteSelection> SelectNodeAsync(string productId, string mode)
        {
            if (_routes.TryGetValue(productId, out var route) == false || route.Candidates.Count == 0)
                return Task.FromResult(RouteSelection.To(string.Empty));

            var ordered = Order(route.Candidates, mode);
            var sched = _schedule.Apply(ordered, c => c.NodeName, SupplierStatusRepository.ROUTING_PRODUCT, productId);

            if (sched.AllClosed == true)
                return Task.FromResult(RouteSelection.Reject(route.Candidates[0].NodeName, sched.ClosedBy?.Id));

            var open = sched.Open;

            if (mode == RoutingMode.LOAD_BALANCE && sched.FirstIsScheduled == false)
            {
                var eligible = open.Where(c => _supplierStatus.IsEligible(c.NodeName) == true).ToList();
                var picked = WeightedPicker.Pick(eligible, c => c.LbWeight);
                if (picked != null)
                    return Task.FromResult(RouteSelection.To(picked.NodeName));
            }

            string node = open.FirstOrDefault(c => _supplierStatus.IsEligible(c.NodeName) == true)?.NodeName;

            return Task.FromResult(RouteSelection.To(node ?? open[0].NodeName));
        }

        private static List<Candidate> Order(List<Candidate> candidates, string mode)
        {
            if (mode == RoutingMode.BEST_PRICE)
            {
                //biller tanpa sharing fee diurutkan paling akhir
                return candidates
                    .OrderByDescending(c => c.FeeSharing.HasValue)
                    .ThenByDescending(c => c.FeeSharing ?? 0)
                    .ThenBy(c => c.Priority)
                    .ToList();
            }

            return candidates.OrderBy(c => c.Priority).ToList();
        }
    }
}
