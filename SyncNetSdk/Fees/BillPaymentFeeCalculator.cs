using SyncNet.Constants;
using SyncNet.DbRepository;
using SyncNet.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using MessageFees = SyncNet.Message.Fees;

namespace SyncNet.Fees
{
    // fee bill payment & purchase dari Product > Fees (sw_fees).
    // urutan pencarian baris: produk + CA + sub CA -> produk + CA -> default produk.
    public class BillPaymentFeeCalculator
    {
        // key = product|merchant|submerchant, kosong untuk level yang tidak diisi
        private volatile Dictionary<string, FeeRuleModel> _rules = [];

        private readonly DbMgr _dbMgr;

        public BillPaymentFeeCalculator()
        {
            _dbMgr = new DbMgr();
        }

        public async Task Initialize()
        {
            var rules = new Dictionary<string, FeeRuleModel>();

            foreach (var obj in await _dbMgr.GetFeesPayment())
            {
                var fee = new FeeRuleModel
                {
                    ProductId = obj.ProductId,
                    MerchantId = obj.MerchantId,
                    SubMerchantId = obj.SubMerchantId,
                    IsFixedFee = obj.IsFixedFee,
                    RoutingMode = obj.RoutingMode,
                    StaticNodeId = obj.StaticNodeId,
                    FixedFeeTotal = obj.FixedFeeTotal,
                    FixedFeeAcq = obj.FixedFeeAcq,
                    FixedFeeMer = obj.FixedFeeMer,
                    FixedFeeIss = obj.FixedFeeIss,
                    FixedFeeBil = obj.FixedFeeBil,
                    FixedFeeSwt = obj.FixedFeeSwt,
                    PercentFeeTotal = obj.PercentFeeTotal,
                    PercentFeeAcq = obj.PercentFeeAcq,
                    PercentFeeMer = obj.PercentFeeMer,
                    PercentFeeIss = obj.PercentFeeIss,
                    PercentFeeBil = obj.PercentFeeBil,
                    PercentFeeSwt = obj.PercentFeeSwt
                };

                //baris pertama menang bila ada duplikat
                rules.TryAdd(RuleKey(fee.ProductId, fee.MerchantId, fee.SubMerchantId), fee);
            }

            //tukar utuh supaya request yang sedang berjalan tidak membaca dictionary setengah jadi
            _rules = rules;
        }

        public FeeRuleModel FindRule(string productId, string merchantId, string subMerchantId = null)
        {
            var rules = _rules;

            if (string.IsNullOrEmpty(merchantId) == false)
            {
                if (string.IsNullOrEmpty(subMerchantId) == false &&
                    rules.TryGetValue(RuleKey(productId, merchantId, subMerchantId), out var sub) == true)
                    return sub;

                if (rules.TryGetValue(RuleKey(productId, merchantId, null), out var mer) == true)
                    return mer;
            }

            return rules.TryGetValue(RuleKey(productId, null, null), out var def) == true ? def : null;
        }

        // routing mode yang berlaku untuk CA ini: dari baris CA bila diisi, kalau kosong
        // (Ikuti Default) dari baris default produk. tanpa baris sama sekali = STATIC.
        public (string Mode, int? StaticNodeId) GetRoutingMode(string productId, string merchantId,
            string subMerchantId = null)
        {
            var rule = FindRule(productId, merchantId, subMerchantId);
            if (rule != null && string.IsNullOrEmpty(rule.RoutingMode) == false)
                return (RoutingMode.Normalize(rule.RoutingMode), rule.StaticNodeId);

            if (_rules.TryGetValue(RuleKey(productId, null, null), out var def) == true &&
                string.IsNullOrEmpty(def.RoutingMode) == false)
                return (RoutingMode.Normalize(def.RoutingMode), def.StaticNodeId);

            return (RoutingMode.STATIC, null);
        }

        // sharingFee diisi untuk mode dynamic: fee biller dan fee switch dihitung dari sharing
        // fee biller siklus ini. null = pakai nilai fee biller/switch yang diinput (STATIC).
        public Task<MessageFees> GetFees(string merchantId, string productId, decimal amount,
            int? sharingFee = null, string subMerchantId = null)
        {
            var result = new MessageFees();

            var rule = FindRule(productId, merchantId, subMerchantId);
            if (rule != null)
                ApplyFeeCalculation(result, rule, amount, sharingFee);

            return Task.FromResult(result);
        }

        private static void ApplyFeeCalculation(MessageFees result, FeeRuleModel f, decimal amount, int? sharingFee)
        {
            if (f.IsFixedFee == true)
            {
                result.total_fee = f.FixedFeeTotal;
                result.acquirer_fee = f.FixedFeeAcq;
                result.merchant_fee = f.FixedFeeMer;
                result.issuer_fee = f.FixedFeeIss;

                if (sharingFee.HasValue == true)
                {
                    //biller menahan sisa admin fee, perusahaan mendapat sisa sharing fee
                    result.biller_fee = f.FixedFeeTotal - sharingFee.Value;
                    result.switch_fee = sharingFee.Value - f.FixedFeeAcq - f.FixedFeeMer - f.FixedFeeIss;
                }
                else
                {
                    result.biller_fee = f.FixedFeeBil;
                    result.switch_fee = f.FixedFeeSwt;
                }
            }
            else
            {
                //persen hanya untuk STATIC (mis. MDR): komponen dihitung dari total fee
                result.total_fee = (f.PercentFeeTotal * amount) / 100;
                result.acquirer_fee = (f.PercentFeeAcq * result.total_fee) / 100;
                result.merchant_fee = (f.PercentFeeMer * result.total_fee) / 100;
                result.issuer_fee = (f.PercentFeeIss * result.total_fee) / 100;
                result.biller_fee = (f.PercentFeeBil * result.total_fee) / 100;
                result.switch_fee = (f.PercentFeeSwt * result.total_fee) / 100;
            }
        }

        private static string RuleKey(string productId, string merchantId, string subMerchantId)
        {
            return $"{productId}|{merchantId}|{subMerchantId}";
        }
    }
}
