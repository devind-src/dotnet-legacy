namespace SyncNet.Models.HsmDeviceModel
{
    public class TranslateKeyResponse : BaseResponse
    {
        public string key_under_lmk { get; set; }
        public string key_check_value { get; set; }
    }
}
