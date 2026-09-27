namespace SyncNet.Models.HsmDeviceModel
{
    public class HsmResponse
    {
        public string message_header { get; set; }
        public string command_response { get; set; }
        public string error_code { get; set; }
        public string data { get; set; }

        public HsmResponse()
        {
            error_code = "99";
        }
    }
}
