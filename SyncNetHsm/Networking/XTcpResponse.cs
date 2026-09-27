namespace SyncNet.Models
{
    public class XTcpResponse
    {
        public bool is_success { get; set; }
        public string error_message { get; set; }
        public string resp_data { get; set; }

        public XTcpResponse()
        {
            is_success = false;
            error_message = "";
            resp_data = "";
        }
    }
}
