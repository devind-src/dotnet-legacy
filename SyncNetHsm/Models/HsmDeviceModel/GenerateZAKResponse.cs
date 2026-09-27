namespace SyncNet.Models.HsmDeviceModel
{
    public class GenerateZAKResponse : BaseResponse
    {
        public string zak_under_lmk { get; set; }
        public string zak_under_zmk { get; set; }
        public string key_check_value { get; set; }
    }
}
