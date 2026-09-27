using SyncNet.Constants;
using SyncNet.DbRepository;
using SyncNet.Fees;
using SyncNet.Models;
using SyncNet.Routing.Failover;
using SyncNet.Routing.Schedule;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SyncNet.Routing
{
    // routing topup (produk di sw_routes_margin). kandidat = supplier aktif per produk + denom
    // dari Supplier Prices. urutan menurut routing mode produk:
    //   STATIC       : supplier yang dipilih (kosong = margin terbesar), tanpa failover
    //   BEST_PRICE   : margin terbesar
    //   PRIORITY     : priority terkecil
    //   LOAD_BALANCE : acak berbobot lb_weight
    // mode dynamic melewati supplier DOWN/SUSPECT; bila semua diblokir tetap ke urutan pertama.
    // Jadwal Routing (Fase 3): supplier Tutup / di luar Jam Operasional dikeluarkan lebih dulu,
    // Prioritas Jadwal didahulukan; tidak ada supplier buka = ditolak. STATIC: supplier pilihan tutup
    // = ditolak. tombol darurat OFF: jadwal diabaikan.
    public class MarginRoutingStrategy : IRoutingStrategy
    {
        private class MarginRoute
        {
            public string Mode;
            public string StaticSupplier;
        }

        private volatile Dictionary<string, MarginRoute> _routeMargin = [];

        private readonly PriceRepository _priceRepository;
        private readonly SupplierStatusRepository _supplierStatus;
        private readonly RoutingScheduleRepository _schedule;
        private readonly DbMgr _dbMgr;

        public MarginRoutingStrategy(PriceRepository priceRepository, SupplierStatusRepository supplierStatus,
            RoutingScheduleRepository schedule)
        {
            _priceRepository = priceRepository;
            _supplierStatus = supplierStatus;
            _schedule = schedule;
            _dbMgr = new DbMgr();
        }

        public async Task Initialize()
        {
            var routes = new Dictionary<string, MarginRoute>();

            foreach (var obj in await _dbMgr.GetRoutesMargin())
            {
                routes.TryAdd(obj.ProductId, new MarginRoute
                {
                    Mode = RoutingMode.Normalize(obj.RoutingMode),
                    StaticSupplier = obj.StaticNodeName
                });
            }

            _routeMargin = routes;
        }

        public bool IsApplicable(string productId)
        {
            return _routeMargin.ContainsKey(productId);
        }

        // failover aktif bila mode produk dynamic dan tombol darurat kategori topup tidak dimatikan
        public bool IsFailoverActive(string productId)
        {
            return _routeMargin.TryGetValue(productId, out var route) == true &&
                RoutingMode.IsDynamic(route.Mode) == true &&
                _supplierStatus.IsFailoverEnabled(SupplierStatusRepository.ROUTING_MARGIN) == true;
        }

        public async Task<string> ResolveSupplierAsync(string productId, long denom)
        {
            return (await SelectSupplierAsync(productId, denom)).NodeName;
        }

        public Task<RouteSelection> SelectSupplierAsync(string productId, long denom)
        {
            if (_priceRepository.TryGetRankedSuppliers(productId, denom, out var ranked) == false ||
                ranked.Count == 0)
                return Task.FromResult(RouteSelection.To(string.Empty));

            _routeMargin.TryGetValue(productId, out var route);
            string mode = route?.Mode ?? RoutingMode.STATIC;

            if (mode == RoutingMode.STATIC)
            {
                //supplier pilihan hanya dipakai bila punya harga aktif untuk denom ini
                string fixedSupplier = route?.StaticSupplier;
                string target = string.IsNullOrEmpty(fixedSupplier) == false && ranked.Any(p => p.SupplierId == fixedSupplier) == true
                    ? fixedSupplier
                    : ranked[0].SupplierId;

                //static tanpa failover: supplier tutup menurut jadwal = ditolak (tombol darurat ON)
                var closedBy = _supplierStatus.IsFailoverEnabled(SupplierStatusRepository.ROUTING_MARGIN) == true
                    ? _schedule.ClosedBy(SupplierStatusRepository.ROUTING_MARGIN, productId, target, _schedule.Now)
                    : null;
                return Task.FromResult(closedBy != null ? RouteSelection.Reject(target, closedBy.Id) : RouteSelection.To(target));
            }

            var ordered = Order(ranked, mode);

            //tombol darurat dimatikan: selalu urutan pertama, status dan jadwal diabaikan
            if (IsFailoverActive(productId) == false)
                return Task.FromResult(RouteSelection.To(ordered[0].SupplierId));

            var sched = _schedule.Apply(ordered, p => p.SupplierId, SupplierStatusRepository.ROUTING_MARGIN, productId);
            if (sched.AllClosed == true)
                return Task.FromResult(RouteSelection.Reject(ordered[0].SupplierId, sched.ClosedBy?.Id));

            var open = sched.Open;

            if (mode == RoutingMode.LOAD_BALANCE && sched.FirstIsScheduled == false)
            {
                var eligible = open.Where(p => _supplierStatus.IsEligible(p.SupplierId) == true).ToList();
                var picked = WeightedPicker.Pick(eligible, p => p.LbWeight);
                if (picked != null)
                    return Task.FromResult(RouteSelection.To(picked.SupplierId));
            }

            //turun menurut urutan, skip supplier yang sedang DOWN/SUSPECT
            foreach (var supplier in open)
            {
                if (_supplierStatus.IsEligible(supplier.SupplierId) == true)
                    return Task.FromResult(RouteSelection.To(supplier.SupplierId));
            }

            //semua supplier yang buka diblokir: tetap coba urutan pertama yang buka
            return Task.FromResult(RouteSelection.To(open[0].SupplierId));
        }

        // ranked sudah urut margin terbesar. PRIORITY dan LOAD_BALANCE mengurutkan ulang
        // menurut priority (kosong di akhir) dengan urutan margin sebagai tie-break.
        private static List<SupplierPriceModel> Order(IReadOnlyList<SupplierPriceModel> ranked, string mode)
        {
            if (mode == RoutingMode.BEST_PRICE)
                return ranked.ToList();

            return ranked
                .Select((p, rank) => (p, rank))
                .OrderBy(x => x.p.Priority ?? int.MaxValue)
                .ThenBy(x => x.rank)
                .Select(x => x.p)
                .ToList();
        }
    }
}
