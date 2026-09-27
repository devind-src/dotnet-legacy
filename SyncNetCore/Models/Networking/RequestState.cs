using System.Net;

namespace SyncNet.Models.Networking
{
    class RequestState
    {
        // This class stores the state of the request.
        public HttpWebRequest webRequest;
        public string msgRequest;

        public RequestState()
        {
            webRequest = null;
            msgRequest = string.Empty;
        }
    }
}
