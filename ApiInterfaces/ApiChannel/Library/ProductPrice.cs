using ApiChannel.Models.Fees;
using SyncNet.DbRepository;
using SyncNet.Message;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApiChannel.Library
{
    internal class ProductPrice
    {
        private readonly Dictionary<string, PricesModel> _lowerPrice = [];
        private readonly Dictionary<string, PricesModel> _supplierPrice = [];
        private readonly Dictionary<string, PricesModel> _merchantPrice = [];

        private readonly Dictionary<string, string> _routeByProduct = [];
        private readonly Dictionary<string, string> _routeDynamic = [];

        private readonly DbMgr _dbMgr;

        public ProductPrice()
        {
            _dbMgr = new DbMgr();
        }

        public async Task Initialize()
        {
            //clear buffer
            _lowerPrice.Clear();
            _supplierPrice.Clear();
            _merchantPrice.Clear();
            _routeByProduct.Clear();
            _routeDynamic.Clear();

            await LoadPriceSupplier();
            await LoadPriceMerchant();
            await LoadRoutingByProduct();
            await LoadRoutingDynamic();
        }
        public  async Task<bool> IsDynamicRouting(string productId)
        {
            return _routeDynamic.ContainsKey(productId);
        }
        public async Task<Fees> GetMarginStatic(string merchantId, string productId, long denom)
        {
            var ret = new Fees();

            //check routing
            if (_routeByProduct.TryGetValue(productId, out var supplier_id) == true)
            {
                //static routing
                ret = await GetMarginStatic(merchantId, productId, denom, supplier_id);
            }

            return ret;
        }
        public async Task<Fees> GetMarginStatic(string merchantId, string productId, long denom, string supplierId)
        {
            Fees ret = new();

            int harga_beli = 0;
            int harga_jual = 0;
            int margin = 0;

            //default margin
            string key = $"{supplierId}:{productId}:{denom}";
            if (_supplierPrice.TryGetValue(key, out PricesModel price) == true)
            {
                harga_beli = price.harga_beli;
                harga_jual = price.harga_jual;
                margin = harga_jual - harga_beli;

                //search price merchant
                key = $"{merchantId}:{productId}:{denom}";
                if (_merchantPrice.TryGetValue(key, out price) == true)
                {
                    //recalculate using price merchant
                    margin = price.harga_jual - harga_beli;
                }
            }

            ret.total_fee = margin;
            ret.switch_fee = margin;
            ret.acquirer_fee = 0;
            ret.biller_fee = 0;

            return ret;
        }
        public async Task<(Fees feesData, string supplierId)> GetMarginDynamic(string merchantId, string productId, long denom)
        {
            var ret = new Fees();

            //check low price from supplier
            string key = $"{productId}:{denom}";
            if (_lowerPrice.TryGetValue(key, out PricesModel price) == false) return (ret, "");

            //calculate margin
            int harga_beli = price.harga_beli;
            int harga_jual = price.harga_jual;
            int margin = harga_jual - harga_beli;

            //check price to merchant
            key = $"{merchantId}:{productId}:{denom}";
            if (_merchantPrice.TryGetValue(key, out price) == true)
            {
                //recalculate margin
                margin = price.harga_jual - harga_beli;
            }

            ret.total_fee = margin;
            ret.switch_fee = margin;
            ret.acquirer_fee = 0;
            ret.biller_fee = 0;

            return (ret, price.supplier_id);
        }
      
        private async Task LoadPriceSupplier()
        {
            var data = await _dbMgr.GetPriceSupplier();

            foreach (var obj in data)
            {
                //skip inactive product
                if (obj.IsActive == false) continue;

                string supplier_id = obj.SupplierId;
                string product_id = obj.ProductId;
                int denom = obj.Denom;

                var pricesModel = new PricesModel
                {
                    supplier_id = supplier_id,
                    harga_beli = obj.PurchasePrice,
                    harga_jual = obj.SellingPrice
                };

                //add price
                string key = $"{supplier_id}:{product_id}:{denom}";
                if (_supplierPrice.ContainsKey(key) == false)
                    _supplierPrice.Add(key, pricesModel);

                //check lower price
                key = $"{product_id}:{denom}";
                if (_lowerPrice.TryGetValue(key, out PricesModel m) == true)
                {
                    if (m.harga_beli > pricesModel.harga_beli)
                    {
                        //update with lower price
                        _lowerPrice.Remove(key);
                        _lowerPrice.Add(key, pricesModel);
                    }
                }
                else
                {
                    _lowerPrice.Add(key, pricesModel);
                }
            }
        }
        private async Task LoadPriceMerchant()
        {
            var data = await _dbMgr.GetPriceMerchant();

            foreach (var obj in data)
            {
                string merchant_id = obj.MerchantId;
                string biller_code = obj.ProductId;
                int denom = obj.Denom;

                var pricesModel = new PricesModel();
                pricesModel.harga_jual = obj.Price;

                string key = $"{merchant_id}:{biller_code}:{denom}";

                //add fee
                if (_merchantPrice.ContainsKey(key) == false)
                    _merchantPrice.Add(key, pricesModel);
            }
        }
        private async Task LoadRoutingByProduct()
        {
            var data = await _dbMgr.GetRoutesByProduct();

            foreach (var obj in data)
            {                
                //add fee
                if (_routeByProduct.ContainsKey(obj.ProductId) == false)
                    _routeByProduct.Add(obj.ProductId, obj.NodeName);
            }
        }
        private async Task LoadRoutingDynamic()
        {
            var data = await _dbMgr.GetRoutesDynamic();

            foreach (var obj in data)
            {
                //add fee
                if (_routeDynamic.ContainsKey(obj.ProductId) == false)
                    _routeDynamic.Add(obj.ProductId, "");
            }
        }
    }
}
