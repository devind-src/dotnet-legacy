using SyncNet.DbRepository;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApiChannel.Library
{
    internal class Product
    {
        private readonly Dictionary<string, string> _data = [];

        private readonly DbMgr _dbMgr;

        public Product()
        {
            _dbMgr = new DbMgr();
        }

        public async Task Initialize()
        {
            //clear buffer
            _data.Clear();

            await LoadTopupProduct();
        }
        public async Task<bool> IsTopup(string productId)
        {
            return _data.ContainsKey(productId);
        }
        private async Task LoadTopupProduct()
        {
            var data = await _dbMgr.GetProductTopup();

            foreach (var obj in data)
            {
                if (_data.ContainsKey(obj.ProductId) == false)
                    _data.Add(obj.ProductId, "");
            }
        }
    }
}
