using ApiChannel.Models.Fees;
using SyncNet.DbRepository;
using SyncNet.Message;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApiChannel.Library
{
    internal class ProductFees
    {
        private readonly Dictionary<string, FeesModel> _feeMerchant = [];
        private readonly Dictionary<string, FeesModel> _feeDefault = [];

        private readonly DbMgr _dbMgr;

        public ProductFees()
        {
            _dbMgr = new DbMgr();
        }
        public async Task Initialize()
        {
            //clear buffer
            _feeMerchant.Clear();
            _feeDefault.Clear();

            await LoadFeesFromDb();
        }
        public async Task<Fees> GetFees(string merchantId, string productId, decimal amount)
        {
            var result = new Fees();

            FeesModel feeModel = null;

            // urutan pencarian: merchant → default
            if (_feeMerchant.TryGetValue($"{productId}:{merchantId}", out var fm))
                feeModel = fm;
            else if (_feeDefault.TryGetValue(productId, out var fd))
                feeModel = fd;

            if (feeModel != null)
                await ApplyFeeCalculation(result, feeModel, amount);

            return result;
        }

        private async Task LoadFeesFromDb()
        {
            var data = await _dbMgr.GetFeesPayment();

            foreach (var obj in data)
            {
                var fee = new FeesModel
                {
                    product_id = obj.ProductId,
                    merchant_id = obj.MerchantId,
                    is_fixed_fee = obj.IsFixedFee,

                    fixed_fee_total = obj.FixedFeeTotal,
                    fixed_fee_acq = obj.FixedFeeAcq,
                    fixed_fee_iss = obj.FixedFeeIss,
                    fixed_fee_swt = obj.FixedFeeSwt,

                    percent_fee_total = obj.PercentFeeTotal,
                    percent_fee_acq = obj.PercentFeeAcq,
                    percent_fee_iss = obj.PercentFeeIss,
                    percent_fee_swt = obj.PercentFeeSwt
                };

                //add fee
                string key;
                if (string.IsNullOrEmpty(fee.merchant_id) == true)
                {
                    key = fee.product_id;

                    if (_feeDefault.ContainsKey(key) == false)
                        _feeDefault.Add(key, fee);
                }
                else
                {
                    key = $"{fee.product_id}:{fee.merchant_id}";

                    if (_feeMerchant.ContainsKey(key) == false)
                        _feeMerchant.Add(key, fee);
                }
            }
        }
        private async Task ApplyFeeCalculation(Fees result, FeesModel f, decimal amount)
        {
            if (f.is_fixed_fee == true)
            {
                result.total_fee = f.fixed_fee_total;
                result.switch_fee = f.fixed_fee_swt;
                result.acquirer_fee = f.fixed_fee_acq;
                result.issuer_fee = f.fixed_fee_iss;
            }
            else
            {
                result.total_fee = (f.percent_fee_total * amount) / 100;
                result.switch_fee = (f.percent_fee_swt * result.total_fee) / 100;
                result.acquirer_fee = (f.percent_fee_acq * result.total_fee) / 100;
                result.issuer_fee = (f.percent_fee_iss * result.total_fee) / 100;
            }
        }
    }
}
