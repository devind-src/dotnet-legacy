namespace SyncNet.Models
{
    public class FeesModel
    {
        public class FeesPayment
        {
            public string ProductId { get; set; }
            public string MerchantId { get; set; }
            public string SubMerchantId { get; set; }
            public bool IsFixedFee { get; set; }
            public string RoutingMode { get; set; }
            public int? StaticNodeId { get; set; }

            public int FixedFeeTotal { get; set; }
            public int FixedFeeAcq { get; set; }
            public int FixedFeeMer { get; set; }
            public int FixedFeeIss { get; set; }
            public int FixedFeeBil { get; set; }
            public int FixedFeeSwt { get; set; }

            public decimal PercentFeeTotal { get; set; }
            public decimal PercentFeeAcq { get; set; }
            public decimal PercentFeeMer { get; set; }
            public decimal PercentFeeIss { get; set; }
            public decimal PercentFeeBil { get; set; }
            public decimal PercentFeeSwt { get; set; }
        }
        public class PriceSupplier
        {
            public string SupplierId { get; set; }
            public string ProductId { get; set; }
            public string ProductName { get; set; }
            public int Denom { get; set; }
            public int PurchasePrice { get; set; }
            public int SellingPrice { get; set; }
            public int Margin { get; set; }
            public bool IsActive { get; set; }
            public int? Priority { get; set; }
            public int LbWeight { get; set; }
        }
        public class PriceMerchant
        {
            public string MerchantId { get; set; }
            public string ProductId { get; set; }
            public string ProductName { get; set; }
            public int Denom { get; set; }
            public int Price { get; set; }
        }
    }
}
