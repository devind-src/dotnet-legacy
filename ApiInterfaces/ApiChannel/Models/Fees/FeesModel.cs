namespace ApiChannel.Models.Fees
{
    internal class FeesModel
    {
        public string product_id { get; set; }
        public string merchant_id { get; set; }
        public bool is_fixed_fee { get; set; }

        public int fixed_fee_total { get; set; }
        public int fixed_fee_acq { get; set; }
        public int fixed_fee_iss { get; set; }
        public int fixed_fee_swt { get; set; }

        public decimal percent_fee_total { get; set; } 
        public decimal percent_fee_acq { get; set; }
        public decimal percent_fee_iss { get; set; }
        public decimal percent_fee_swt { get; set; }
    }
}
