namespace SyncNet.Models
{
    public class FeeRuleModel
    {
        public string ProductId { get; set; }
        public string MerchantId { get; set; }
        public string SubMerchantId { get; set; }
        public bool IsFixedFee { get; set; }

        // null di baris CA = ikuti baris default produk
        public string RoutingMode { get; set; }
        public int? StaticNodeId { get; set; }

        public int FixedFeeTotal { get; set; }
        public int FixedFeeAcq { get; set; }    // loket
        public int FixedFeeMer { get; set; }    // mitra
        public int FixedFeeIss { get; set; }
        public int FixedFeeBil { get; set; }    // biller (diinput bila STATIC)
        public int FixedFeeSwt { get; set; }    // perusahaan (diinput bila STATIC)

        public decimal PercentFeeTotal { get; set; }
        public decimal PercentFeeAcq { get; set; }
        public decimal PercentFeeMer { get; set; }
        public decimal PercentFeeIss { get; set; }
        public decimal PercentFeeBil { get; set; }
        public decimal PercentFeeSwt { get; set; }
    }
}
