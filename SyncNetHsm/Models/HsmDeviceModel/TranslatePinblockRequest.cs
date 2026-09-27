namespace SyncNet.Models.HsmDeviceModel
{
    public class TranslatePinblockRequest
    {
        public string key_under_lmk_source { get; set; }
        public string key_under_lmk_dest { get; set; }
        public string source_pinblock { get; set; }
        public string account_number { get; set; }
    }
}
