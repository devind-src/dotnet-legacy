namespace SyncNet.Models.HsmDeviceModel
{
    public class GenerateTPKResponse : BaseResponse
    {
        public string key_under_lmk { get; set; }
        public string key_under_tmk { get; set; }
        public string key_check_value { get; set; }
    }
}
