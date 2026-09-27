namespace SyncNet.Models.Networking
{
    public class TcpResponse
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public string RespData { get; set; }

        public TcpResponse()
        {
            IsSuccess = false;
            ErrorMessage = "";
            RespData = "";
        }
    }
}
