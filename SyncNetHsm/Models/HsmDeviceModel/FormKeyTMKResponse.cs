namespace SyncNet.Models.HsmDeviceModel
{
    public class FormKeyTMKResponse : BaseResponse
    {
        public string tmk_under_lmk { get; set; }
        public string key_check_value { get; set; }
    }
}
