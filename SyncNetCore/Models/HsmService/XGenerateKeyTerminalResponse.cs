namespace SyncNet.Models.HsmService
{
    public class XGenerateKeyTerminalResponse : BaseResponse
    {
        public string key_under_tmk { get; set; }
        public string key_check_value { get; set; }
    }
}
