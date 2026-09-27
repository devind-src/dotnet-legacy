namespace SyncNet.Message
{
    public class Fees
    {
        public decimal total_fee { get; set; }
        public decimal acquirer_fee { get; set; }
        public decimal merchant_fee { get; set; }
        public decimal submerchant_fee { get; set; }
        public decimal switch_fee { get; set; }
        public decimal biller_fee { get; set; }
        public decimal issuer_fee { get; set; }
    }
}
