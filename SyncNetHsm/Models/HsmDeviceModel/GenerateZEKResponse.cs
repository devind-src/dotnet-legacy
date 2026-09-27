namespace SyncNet.Models.HsmDeviceModel
{
    public class GenerateZEKResponse : BaseResponse
    {
        public string zek_under_lmk { get; set; }
        public string zek_under_zmk { get; set; }
        public string key_check_value { get; set; }
    }
}
