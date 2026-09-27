namespace SyncNet.Models.HsmDeviceModel
{
    public class GenerateMACRequest
    {
        public string key_type { get; set; } //0-TAK, 1 ZAK
        public string key_under_lmk { get; set; }
        public string data_base64 { get; set; }
    }
}
