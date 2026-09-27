using SyncNet.DbRepository;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SyncNet.Routing
{
    // routing berdasarkan tabel produk -> node/supplier tetap
    public class StaticRoutingStrategy : IRoutingStrategy
    {
        private readonly Dictionary<string, string> _routeByProduct = [];

        private readonly DbMgr _dbMgr;

        public StaticRoutingStrategy()
        {
            _dbMgr = new DbMgr();
        }

        public async Task Initialize()
        {
            _routeByProduct.Clear();

            await LoadRoutingByProduct();
        }

        public bool IsApplicable(string productId)
        {
            return _routeByProduct.ContainsKey(productId);
        }

        public Task<string> ResolveSupplierAsync(string productId, long denom)
        {
            _routeByProduct.TryGetValue(productId, out var supplierId);

            return Task.FromResult(supplierId);
        }

        private async Task LoadRoutingByProduct()
        {
            var data = await _dbMgr.GetRoutesByProduct();

            foreach (var obj in data)
            {
                //add route
                if (_routeByProduct.ContainsKey(obj.ProductId) == false)
                    _routeByProduct.Add(obj.ProductId, obj.NodeName);
            }
        }
    }
}
