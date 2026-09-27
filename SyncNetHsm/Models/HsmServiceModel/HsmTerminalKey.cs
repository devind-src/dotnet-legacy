namespace SyncNet.Models.HsmServiceModel
{
    public class HsmTerminalKey
    {
        public string merchant_id { get; set; }
        public string serial_number { get; set; }
        public string tmk_under_lmk { get; set; }
        public string key_under_lmk { get; set; }
        public string key_under_tmk { get; set; }
        public string key_check_value { get; set; }
        public string status { get; set; }
    }
}
