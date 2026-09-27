namespace SyncNet.Models.HsmDeviceModel
{
    public class GenerateZPKResponse : BaseResponse
    {
        public string key_under_lmk { get; set; }
        public string key_under_zmk { get; set; }
        public string key_check_value { get; set; }
    }
}
