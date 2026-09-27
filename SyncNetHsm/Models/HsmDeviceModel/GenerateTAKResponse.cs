namespace SyncNet.Models.HsmDeviceModel
{
    public class GenerateTAKResponse : BaseResponse
    {
        public string tak_under_lmk { get; set; }
        public string tak_under_zmk { get; set; }
        public string key_check_value { get; set; }
    }
}
