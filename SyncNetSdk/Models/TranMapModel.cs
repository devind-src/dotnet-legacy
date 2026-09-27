namespace SyncNet.Models
{
    // satu baris sw_routes_tran_map: biller yang dipilih untuk satu siklus transaksi
    public class TranMapModel
    {
        public long Id { get; set; }
        public string MerchantId { get; set; }
        public string TerminalId { get; set; }
        public string Refnum { get; set; }
        public string RoutingType { get; set; }
        public string InstId { get; set; }
        public int? Denom { get; set; }
        public string NodeName { get; set; }
        public string InquirySwitchKey { get; set; }
        public string SwitchKey { get; set; }
    }
}
