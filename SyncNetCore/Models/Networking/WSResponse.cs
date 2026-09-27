using System.Net;

namespace SyncNet.Models.Networking
{
    public class WSResponse
    {
        public string MsgResponse { get; set; }
        public string MsgError { get; set; }
        public HttpStatusCode StatusCode { get; set; }
    }
}
