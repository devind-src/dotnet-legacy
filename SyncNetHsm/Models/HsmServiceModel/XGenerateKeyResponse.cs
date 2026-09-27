namespace SyncNet.Models.HsmServiceModel
{
    public class XGenerateKeyResponse : BaseResponse
    {
        public string key_under_zmk { get; set; }
        public string key_check_value { get; set; }
    }
}
