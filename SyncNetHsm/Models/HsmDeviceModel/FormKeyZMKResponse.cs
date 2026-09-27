namespace SyncNet.Models.HsmDeviceModel
{
    public class FormKeyZMKResponse : BaseResponse
    {
        public string zmk_under_lmk { get; set; }
        public string key_check_value { get; set; }
    }
}
