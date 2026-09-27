namespace SyncNet.Models.HsmServiceModel
{
    public class HsmNodeKey
    {
        public string node_name { get; set; }
        public string zmk_under_lmk { get; set; }
        public string key_under_lmk { get; set; }
        public string key_under_zmk { get; set; }
        public string key_check_value { get; set; }
        public string pinblock_format { get; set; }
    }
}
