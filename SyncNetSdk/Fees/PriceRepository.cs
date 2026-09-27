using SyncNet.DbRepository;
using SyncNet.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SyncNet.Fees
{
    public class PriceRepository
    {
        private readonly Dictionary<string, List<SupplierPriceModel>> _rankedSuppliers = [];
        private readonly Dictionary<string, SupplierPriceModel> _supplierPrice = [];
        private readonly Dictionary<string, int> _merchantSellingPrice = [];

        private readonly DbMgr _dbMgr;

        public PriceRepository()
        {
            _dbMgr = new DbMgr();
        }

        public async Task Initialize()
        {
            //clear buffer
            _rankedSuppliers.Clear();
            _supplierPrice.Clear();
            _merchantSellingPrice.Clear();

            await LoadPriceSupplier();
            await LoadPriceMerchant();
        }

        // urutan rank 1, 2, 3, dst berdasarkan margin terbesar (harga_jual - harga_beli).
        // dipakai oleh MarginRoutingStrategy: mode BEST_PRICE memakai urutan ini apa adanya,
        // mode lain mengurutkan ulang dari daftar ini.
        public bool TryGetRankedSuppliers(string productId, long denom, out IReadOnlyList<SupplierPriceModel> suppliers)
        {
            if (_rankedSuppliers.TryGetValue($"{productId}:{denom}", out var list) == false)
            {
                suppliers = null;
                return false;
            }

            suppliers = list;
            return true;
        }

        public bool TryGetSupplierPrice(string supplierId, string productId, long denom, out SupplierPriceModel price)
        {
            return _supplierPrice.TryGetValue($"{supplierId}:{productId}:{denom}", out price);
        }

        public bool TryGetMerchantSellingPrice(string merchantId, string productId, long denom, out int sellingPrice)
        {
            return _merchantSellingPrice.TryGetValue($"{merchantId}:{productId}:{denom}", out sellingPrice);
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

                var priceModel = new SupplierPriceModel
                {
                    SupplierId = supplier_id,
                    PurchasePrice = obj.PurchasePrice,
                    SellingPrice = obj.SellingPrice,
                    Margin = obj.Margin,
                    Priority = obj.Priority,
                    LbWeight = obj.LbWeight
                };

                //add price
                string key = $"{supplier_id}:{product_id}:{denom}";
                if (_supplierPrice.ContainsKey(key) == false)
                    _supplierPrice.Add(key, priceModel);

                //group per product:denom, di-rank belakangan setelah semua data masuk
                key = $"{product_id}:{denom}";
                if (_rankedSuppliers.TryGetValue(key, out var list) == false)
                {
                    list = [];
                    _rankedSuppliers.Add(key, list);
                }

                list.Add(priceModel);
            }

            // rank 1 = margin terbesar. tie-break: harga_beli termurah, lalu supplier_id
            // supaya urutan deterministik kalau margin & harga_beli sama.
            foreach (var key in _rankedSuppliers.Keys.ToList())
            {
                _rankedSuppliers[key] = _rankedSuppliers[key]
                    .OrderByDescending(p => p.Margin)
                    .ThenBy(p => p.PurchasePrice)
                    .ThenBy(p => p.SupplierId)
                    .ToList();
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

                string key = $"{merchant_id}:{biller_code}:{denom}";

                //add price
                if (_merchantSellingPrice.ContainsKey(key) == false)
                    _merchantSellingPrice.Add(key, obj.Price);
            }
        }
    }
}
